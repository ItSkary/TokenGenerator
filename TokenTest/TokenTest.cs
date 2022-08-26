using Microsoft.VisualStudio.TestTools.UnitTesting;
using SecurityDriven.Inferno;
using SecurityDriven.Inferno.Extensions;
using System;
using System.Linq;
using System.Security;
using Token;

namespace TokenTest
{
    [TestClass]
    public class TokenTest
    {
        private SecureString _masterKey = new SecureString();
        private int _tokenSize = 256;

        [TestInitialize]
        public void Setup ()
        {
            _masterKey.AppendChar('M');
            _masterKey.AppendChar('a');
            _masterKey.AppendChar('s');
            _masterKey.AppendChar('t');
            _masterKey.AppendChar('e');
            _masterKey.AppendChar('r');
            _masterKey.AppendChar('K');
            _masterKey.AppendChar('e');
            _masterKey.AppendChar('y');
        }

        [TestMethod]
        public void _000_TokenFactory_IstantiateBeforeSetup()
        {
            Assert.ThrowsException<InvalidOperationException>(GenericTokenFactory.Instance);
        }

        [TestMethod]
        public void _010_TokenFactorySetup()
        {
            bool status = GenericTokenFactory.Setup(_masterKey,tokenSize : _tokenSize);

            Assert.IsTrue(status);
        }

        [TestMethod]
        public void _020_TokenFactorySetup_DoubleSetupDoesNotChangeData()
        {
            bool status = GenericTokenFactory.Setup(_masterKey, tokenSize: 100);

            Assert.IsFalse(status);
        }

        [TestMethod]
        public void _030_TokenFactoryIstantiate()
        {
            GenericTokenFactory tokenFactory = GenericTokenFactory.Instance();

            Assert.IsTrue(tokenFactory != null && tokenFactory is GenericTokenFactory);
        }

        [TestMethod]
        public void _040_TokenFactoryCreateToken()
        {
            GenericTokenFactory tokenFactory = GenericTokenFactory.Instance();
            GenericToken token = tokenFactory.Create();

            Assert.IsTrue(token != null);
            Assert.IsTrue(token.ToDynamic.__ == "standard");
            Assert.IsTrue(token.ToDynamic["__"] == "standard");
            Assert.IsTrue(token.ToDynamic[0] == "standard");
            Assert.IsTrue(token.ToDynamic.__size == _tokenSize.ToString() );
            Assert.IsTrue(token.ToDynamic.__timeoutMs == 600_000.ToString());
        }

        [TestMethod]
        public void _050_Token_ReadInvalidIndex()
        {
            GenericTokenFactory tokenFactory = GenericTokenFactory.Instance();
            GenericToken token = tokenFactory.Create();

            Assert.ThrowsException<IndexOutOfRangeException>(() => token.ToDynamic[-1]);
        }

        [TestMethod]
        public void _060_Token_ReadInvalidProperty()
        {
            GenericTokenFactory tokenFactory = GenericTokenFactory.Instance();
            GenericToken token = tokenFactory.Create();

            Assert.IsTrue(token.ToDynamic.FictionalProperity == null);
        }

        [TestMethod]
        public void _070_Token_SetProperty()
        {
            GenericTokenFactory tokenFactory = GenericTokenFactory.Instance();
            GenericToken token = tokenFactory.Create();
            token.ToDynamic.MyProperty = "MyValue";

            Assert.IsTrue(token.ToDynamic.MyProperty == "MyValue");
            Assert.IsTrue(token.ToDynamic["MyProperty"] == "MyValue");
        }

        [TestMethod]
        public void _071_Token_SetPropertyWithInvalidType()
        {
            GenericTokenFactory tokenFactory = GenericTokenFactory.Instance();
            GenericToken token = tokenFactory.Create();

            Assert.ThrowsException<ArgumentException>( () => token.ToDynamic.MyProperty = new object());
        }

        [TestMethod]
        public void _080_Token_ChangeProperty()
        {
            GenericTokenFactory tokenFactory = GenericTokenFactory.Instance();
            GenericToken token = tokenFactory.Create();
            token.ToDynamic.MyProperty = "MyValue";

            Assert.IsTrue(token.ToDynamic.MyProperty == "MyValue");

            token.ToDynamic.MyProperty = "MyNew␝Val␜ue";
            Assert.IsTrue(token.ToDynamic.MyProperty == "MyNewValue");

            token.ToDynamic[4] = "MyNew␝Va␜lue2";
            Assert.IsTrue(token.ToDynamic.MyProperty == "MyNewValue2");
            Assert.IsTrue(token.ToDynamic[4] == "MyNewValue2");
        }

        [TestMethod]
        public void _090_Token_ToString()
        {
            GenericTokenFactory tokenFactory = GenericTokenFactory.Instance();
            GenericToken token = tokenFactory.Create();
            token.ToDynamic.MyProperty = "MyValue";

            string tokenAsString = token.ToString();
            Assert.IsTrue(tokenAsString.IndexOf($"__␝standard␜__creationTime␝") == 0 );
            Assert.IsTrue(tokenAsString.Contains("__size"));
            Assert.IsTrue(tokenAsString.Contains(_tokenSize.AsInvariantString()));
            Assert.IsTrue(tokenAsString.Contains("MyProperty"));
            Assert.IsTrue(tokenAsString.Contains("MyValue"));
            Assert.IsTrue(tokenAsString.Length == _tokenSize);
        }

        [TestMethod]
        public void _100_Token_Clone()
        {
            GenericTokenFactory tokenFactory = GenericTokenFactory.Instance();
            GenericToken token = tokenFactory.Create();
            token.ToDynamic.MyProperty = "MyValue";

            GenericToken clone = token.Clone();

            string originalAsString = token.ToString();
            string originalAsSrtingWithoutPadding = originalAsString.Substring(0, originalAsString.LastIndexOf("␜"));
            string clonedAsString = clone.ToString();
            string clonedAsStringWithoutPadding = clonedAsString.Substring(0, clonedAsString.LastIndexOf("␜"));

            Assert.IsTrue(originalAsSrtingWithoutPadding.Equals(clonedAsStringWithoutPadding) );//remove random padding in the comparison

            string[] originalMembers = token.GetDynamicMemberNames().ToArray();
            string[] clonedMembers   = clone.GetDynamicMemberNames().ToArray();

            bool hasSameMembers = true;
            foreach(string orginalMember in originalMembers)
                if ( clonedMembers.Any( member => member.Equals(orginalMember) ) == false)
                {
                    hasSameMembers = false;
                    break;
                }

            Assert.IsTrue(hasSameMembers);
        }

        [TestMethod]
        public void _110_Token_BoundariesCheck()
        {
            GenericTokenFactory tokenFactory = GenericTokenFactory.Instance();
            GenericToken token = tokenFactory.Create();
            token.ToDynamic.MyProperty = "MyValue";

            Assert.IsTrue(token.CheckBoundaries());

            token.ToDynamic.MyHugePorperty = "0YzyGhcSt8M5J4aNOD2gM6qTZYicLkjn3phVOHoT2vC5EmpD5aF8NdnwZpwAjljUUHb9kUmikOkwYB7yDbHXbKoQaAGTf9Enzx9QPCx5GPbTYZI50N8dzihoQRNMiATYEyNootQE1jYaW0trD8hqkyJPvxBLEc2ebVekWmxTdhfPSZ0fICxszqYl3njEZxDcAxuqeZoQn3XAeFe0P5ym3qFSew1eQlIyyISMzZ5rQBD1ZMBLaRawnrmkL2LdIqKh";

            Assert.IsFalse(token.CheckBoundaries());
        }

        [TestMethod]
        public void _120_Token_EvaluateExpiration()
        {
            GenericTokenFactory tokenFactory = GenericTokenFactory.Instance();
            GenericToken token = tokenFactory.Create();
            token.ToDynamic.MyProperty = "MyValue";

            Assert.IsFalse(token.IsExpired());//check the default expiration of ten minutes starting from ceration

            System.Threading.Thread.Sleep(50);

            Assert.IsTrue(token.IsExpired( timeoutMs : 5));//check again redefining timeout of 5ms
        }

        [TestMethod]
        public void _130_TokenFactory_CreateFromString()
        {
            GenericTokenFactory tokenFactory = GenericTokenFactory.Instance();
            GenericToken token = tokenFactory.Create();
            token.ToDynamic.MyProperty = "MyValue";

            GenericToken copy = tokenFactory.Create(plainTextTokenData: token.ToString());


            string originalAsString = token.ToString();
            string originalAsSrtingWithoutPadding = originalAsString.Substring(0, originalAsString.LastIndexOf("␜"));
            string copiedAsString = copy.ToString();
            string copiedAsStringWithoutPadding = copiedAsString.Substring(0, copiedAsString.LastIndexOf("␜"));

            Assert.IsTrue(originalAsSrtingWithoutPadding.Equals(copiedAsStringWithoutPadding));//remove random padding in the comparison

            string[] originalMembers = token.GetDynamicMemberNames().ToArray();
            string[] clonedMembers = copy.GetDynamicMemberNames().ToArray();

            bool hasSameMembers = true;
            foreach (string orginalMember in originalMembers)
                if (clonedMembers.Any(member => member.Equals(orginalMember)) == false)
                {
                    hasSameMembers = false;
                    break;
                }

            Assert.IsTrue(hasSameMembers);
        }

        [TestMethod]
        public void _140_TokenFactory_EncryptDecrypt()
        {
            GenericTokenFactory tokenFactory = GenericTokenFactory.Instance();
            GenericToken token = tokenFactory.Create();
            token.ToDynamic.MyProperty = "MyValue";

            string encryptedString = token.Encrypt();
            GenericToken encryptedDecrypted = tokenFactory.Decrypt(encryptedString);


            string originalAsString = token.ToString();
            string originalAsSrtingWithoutPadding = originalAsString.Substring(0, originalAsString.LastIndexOf("␜"));
            string ecdcAsString = encryptedDecrypted.ToString();
            string ecdcAsStringWithoutPadding = ecdcAsString.Substring(0, ecdcAsString.LastIndexOf("␜"));

            Assert.IsTrue(originalAsSrtingWithoutPadding.Equals(ecdcAsStringWithoutPadding));//remove random padding in the comparison

            string[] originalMembers = token.GetDynamicMemberNames().ToArray();
            string[] clonedMembers = encryptedDecrypted.GetDynamicMemberNames().ToArray();

            bool hasSameMembers = true;
            foreach (string orginalMember in originalMembers)
                if (clonedMembers.Any(member => member.Equals(orginalMember)) == false)
                {
                    hasSameMembers = false;
                    break;
                }

            Assert.IsTrue(hasSameMembers);
        }

    }
}
