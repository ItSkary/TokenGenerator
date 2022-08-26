using SecurityDriven.Inferno;
using SecurityDriven.Inferno.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;

namespace Token
{
    public class GenericTokenFactory
    {
        /// <summary>
        /// A pepper value to bind keys to this program : https://en.wikipedia.org/wiki/Pepper_(cryptography)
        /// </summary>
        private static readonly byte[] _PEPPER = new byte[] { 0x78, 0x64, 0x70, 0x71, 0x36, 0x73, 0x7a, 0x63, 0x55, 0x50, 0x73, 0x32, 0x61, 0x36, 0x65, 0x4b, 0x4d, 0x66, 0x38, 0x37, 0x41 };

        private static object _sync = new object();
        private static GenericTokenFactory _instance = null;

        private static bool _setupHappened = false;
        private static int _tokenSize = 0;
        private static int _timeoutMs = 600_000; //10 minutes 
        private static SecureString _masterKey = null;

        /// <summary>
        /// Setup configuration to get instance of GenericTokenFactory
        /// </summary>
        /// <param name="masterKey">A SecureString used to store masterKey</param>
        /// <param name="tokenSize">The configuration to set current token size (all token have fixed size regardeless of the content) (optional : default=128)</param>
        /// <param name="timeoutMs">The configuration to set current token timeout expressed in ms (optional : default=600000 equal to 10 miuntes)</param>
        /// <remarks>Setup method is applied only once</remarks>
        /// <return>True if the data are applied, false otherwise (data are ignored if a previous setup already happened)</return>
        public static bool Setup ( SecureString masterKey, int tokenSize = 0, int timeoutMs = 600_000)
        {
            if (_setupHappened)
                return false;

            lock (_sync)
            {
                if (_setupHappened)
                    return false;

                _tokenSize = tokenSize;
                _timeoutMs = timeoutMs;
                _masterKey = GenericTokenFactory.AccessSecuredData<SecureString>(masterKey, _deriveKeyDelegateRef);
                _setupHappened = true;

                return true;
            }
        }

        /// <summary>
        /// Create or get an instance of GenericTokenFactory
        /// </summary>
        /// <returns>An instance of GenericTokenFactory</returns>
        public static GenericTokenFactory Instance()
        {
            if ( _setupHappened == false)
                throw new InvalidOperationException("Execute setup operation first");

            if (_instance != null)
                return _instance;

            lock (_sync)
            {
                if (_instance != null)
                    return _instance;

                if (_masterKey == null)
                    throw new InvalidOperationException("Setup the MasterKey first");

                _instance = new GenericTokenFactory();
                return _instance;
            }
        }

        private GenericTokenFactory(){}

        /// <summary>
        /// Create a new instance of GnericToken
        /// </summary>
        /// <returns>The new instnace of GnericToken</returns>
        public GenericToken Create ()
        {
            return new GenericToken(this, size : _tokenSize, timeoutMs : _timeoutMs);
        }

        /// <summary>
        /// Create a new instance of GenericTokne starting from its string representation (in plain text)
        /// </summary>
        /// <param name="plainTextTokenData"></param>
        /// <returns></returns>
        public GenericToken Create(string plainTextTokenData)
        {
            return new GenericToken(this, tokenData: plainTextTokenData);
        }

        /// <summary>
        /// Decrypt the supplid string (representing an encrypted token) and return the GenericToken represented
        /// </summary>
        /// <param name="encryptedTokenData">Encrypted string that represent a GenericToken</param>
        /// <returns>The GenericToken represented by encrypted string</returns>
        public GenericToken Decrypt (string encryptedTokenData)
        {
            return this.Create (GenericTokenFactory.AccessSecuredData<string>(_masterKey,_decryptDelegateRef, encryptedTokenData));
        }

        /// <summary>
        /// Encrypt the supplied GenericToken
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        internal string Encrypt ( GenericToken token)
        {
            return GenericTokenFactory.AccessSecuredData<string>(_masterKey, _encryptDelegateRef, token.ToString());
        }



        /// <summary>
        /// Execute the <paramref name="operationDelegate"/> provided passing the decoded <paramref name="secureString"/> as first argument of type byte array
        /// </summary>
        /// <typeparam name="TReturn">The delegate return type</typeparam>
        /// <param name="secureString">The SecureString to decode</param>
        /// <param name="operationDelegate">The delegate with the logic to execute</param>
        /// <param name="parameters">The parameter to povide to the <paramref name="operationDelegate"/></param>
        /// <returns>The return value of the delegate</returns>
        /// <remarks>The <paramref name="operationDelegate"/> has to take a first parameter of type byte array</remarks>
        private static TReturn AccessSecuredData<TReturn> (SecureString secureString, Delegate operationDelegate , params object[] parameters)
        {
            var pUnicodeBytes = Marshal.SecureStringToGlobalAllocUnicode(secureString);
            byte[] workArray = null;
            try
            {
                workArray = new byte[secureString.Length * 2];

                for (var idx = 0; idx < workArray.Length; ++idx)
                {
                    workArray[idx] = Marshal.ReadByte(pUnicodeBytes, idx);
                }

                List<object> actualParameters = new List<object>(parameters ?? new object[0]);
                actualParameters.Insert(0, workArray);

                return (TReturn)operationDelegate.DynamicInvoke(actualParameters.ToArray());
            }
            finally
            {
                if (workArray != null)
                    for (int i = 0; i < workArray.Length; i++)
                        workArray[i] = 0;

                Marshal.ZeroFreeGlobalAllocUnicode(pUnicodeBytes);
            }
        }


        /// <summary>
        /// Delegate to represent decrypt operation
        /// </summary>
        /// <param name="key">The key to use to dencrypt data</param>
        /// <param name="data">The encrypted text</param>
        /// <returns></returns>
        private delegate string DecryptDelegate(byte[] orignalKey, string data);
        /// <summary>
        /// Delegate reference to Decrypt method
        /// </summary>
        private static Delegate _decryptDelegateRef = new DecryptDelegate(Decrypt);
        /// <summary>
        /// Decrypt the token data with the current key
        /// </summary>
        /// <param name="key">The key to use to decrypt data</param>
        /// <param name="data">The encrypted text</param>
        /// <returns></returns>
        private static string Decrypt(byte[] key, string data)
        {
            return SuiteB.Decrypt(key, data.FromB64().AsArraySegment()).FromBytes();
        }


        /// <summary>
        /// Delegate to represent encrypt operation
        /// </summary>
        /// <param name="key">The key to use to encrypt data</param>
        /// <param name="data">The plain text</param>
        /// <returns></returns>
        private delegate string EncryptDelegate(byte[] derivedKey, string data);
        /// <summary>
        /// Delegate reference to Encrypt method
        /// </summary>
        private static Delegate _encryptDelegateRef = new EncryptDelegate(Encrypt);
        /// <summary>
        /// Encrypt the token data with the current key
        /// </summary>
        /// <param name="derivedKey">The key to use to encrypt data</param>
        /// <param name="data">The plain text</param>
        /// <returns></returns>
        private static string Encrypt ( byte[] derivedKey, string data )
        {
            var byteArray = SuiteB.Encrypt(derivedKey, data.ToBytes().AsArraySegment());
            return byteArray.ToB64();
        }


        /// <summary>
        /// Delegate to represent key derivation operation
        /// </summary>
        /// <param name="orignalKey"></param>
        /// <returns></returns>
        private delegate SecureString DeriveKeyDelegate(byte[] orignalKey);
        /// <summary>
        /// Delegate reference to DeriveKey method
        /// </summary>
        private static Delegate _deriveKeyDelegateRef = new DeriveKeyDelegate(DeriveKey);
        /// <summary>
        /// Derive the current key from the master key. 
        /// </summary>
        /// <param name="originalKey">The original master key</param>
        /// <returns>A SecureString containing the derived key</returns>
        private static SecureString DeriveKey ( byte[] originalKey )
        {
            //TODO : evaluate a way to make the derived key a bit more ephimeral so that can be changed frequently without affecting already generated token
            using (var hmac = SuiteB.HmacFactory())
            {
                hmac.Key = originalKey;
                byte [] derived = hmac.ComputeHash(_PEPPER) ;

                SecureString result = new SecureString();
                foreach (byte character in derived)
                    result.AppendChar((char)character);

                return result;
            }
        }

    }
}
