using SecurityDriven.Inferno;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;

namespace Token
{
    public class GenericToken : DynamicObject
    {
        private const int _DEF_SIZE = 128;
        private const int _DEF_EXPIRATION_MS = 36_000_000; // 10h
        private const char _NAME_VALUE_SEPARATOR = '␝';
        private const char _ITEMS_SEPARATOR = '␜';
        private const string _DEF_KEY = "__";
        private const string _DEF_KEY_VAL = "standard";
        private const string _SYS_KEY_SIZE = "__size";
        private const string _SYS_KEY_CREATION_TIME = "__creationTime";
        private const string _SYS_KEY_TIMEOUTMS = "__timeoutMs";

        private static readonly string[] _SYS_KEYS = new string[] { _DEF_KEY, _SYS_KEY_SIZE, _SYS_KEY_CREATION_TIME, _SYS_KEY_TIMEOUTMS };

        private GenericTokenFactory _factoryRef = null;
        private ConcurrentDictionary<string, string> _properties = new ConcurrentDictionary<string, string>();

        public dynamic ToDynamic { get { return (dynamic)this; } }

        /// <summary>
        /// Private constructor used for cloning operation
        /// </summary>
        /// <param name="factory">Factory reference form instance to clone</param>
        /// <param name="properties">Properties of the instance to clone</param>
        private GenericToken(GenericTokenFactory factory, ConcurrentDictionary<string, string> properties)
        {
            if (factory == null)
                throw new ArgumentNullException("factory");

            if (properties == null)
                throw new ArgumentNullException("properties");

            _factoryRef = factory;

            foreach (string key in properties.Keys)
                if (_properties.ContainsKey(key) == false)
                    _properties.TryAdd(key, properties[key]);
        }

        /// <summary>
        /// Class constructor
        /// </summary>
        /// <param name="factory">Reference to the factory that build that instance</param>
        /// <param name="size">Size of the current instance when represented as string</param>
        /// <param name="timeoutMs">Size of the current instance when represented as string</param>
        internal GenericToken(GenericTokenFactory factory, int size = 0, int timeoutMs = _DEF_EXPIRATION_MS)
        {
            if (factory == null)
                throw new ArgumentNullException("factory");

            if (timeoutMs <= 0)
                timeoutMs = _DEF_EXPIRATION_MS;

            _factoryRef = factory;

            if (size <= 0)
                size = _DEF_SIZE;

            //can not be assigned thorugh dynamic access because this property is read-only
            _properties[_DEF_KEY]               = _DEF_KEY_VAL; 
            _properties[_SYS_KEY_SIZE]          = size.AsInvariantString();
            _properties[_SYS_KEY_CREATION_TIME] = DateTime.Now.Ticks.AsInvariantString();
            _properties[_SYS_KEY_TIMEOUTMS]     = timeoutMs.AsInvariantString();
        }

        #region Not Supported Methods
        public override bool TryDeleteIndex(DeleteIndexBinder binder, object[] indexes)
        {
            throw new NotSupportedException();
        }

        public override bool TryDeleteMember(DeleteMemberBinder binder)
        {
            throw new NotSupportedException();
        }

        public override bool TryInvoke(InvokeBinder binder, object[] args, out object result)
        {
            throw new NotSupportedException();
        }

        public override bool TryInvokeMember(InvokeMemberBinder binder, object[] args, out object result)
        {
            throw new NotSupportedException();
        }

        public override bool TryBinaryOperation(BinaryOperationBinder binder, object arg, out object result)
        {
            throw new NotSupportedException();
        }

        public override bool TryConvert(ConvertBinder binder, out object result)
        {
            throw new NotSupportedException();
        }

        public override bool TryCreateInstance(CreateInstanceBinder binder, object[] args, out object result)
        {
            throw new NotSupportedException();
        }

        public override bool TryUnaryOperation(UnaryOperationBinder binder, out object result)
        {
            throw new NotSupportedException();
        }
        #endregion

        /// <summary>
        /// Create a new instance starting from an existing token representation as tring
        /// </summary>
        /// <param name="factory">Reference to the factory that build that instance</param>
        /// <param name="tokenData">The plaintext representation of data inside the current token</param>
        internal GenericToken(GenericTokenFactory factory, string tokenData)
        {
            if (factory == null)
                throw new ArgumentNullException("factory");

            if ( string.IsNullOrWhiteSpace(tokenData) )
                throw new ArgumentNullException("tokenData");

            if ( tokenData.IndexOf(_NAME_VALUE_SEPARATOR) <= 0|| tokenData.IndexOf(_ITEMS_SEPARATOR) <= 0)
                throw new ArgumentException("tokenData");

            if (_properties == null)
                throw new InvalidOperationException("Current object internal state is not valid");

            _factoryRef = factory;

            string[] keypairs = tokenData.Split(new char[] { _ITEMS_SEPARATOR }, StringSplitOptions.RemoveEmptyEntries);
            int keyPairCounter = 1;
            foreach (string keypair in keypairs)
            {
                int separatorIndex = keypair.IndexOf(_NAME_VALUE_SEPARATOR);

                if (separatorIndex <= 0 && keyPairCounter != keypairs.Length)
                    throw new ArgumentException("Invalid parameter in the data", "tokenData");
                else if (separatorIndex <= 0 && keyPairCounter == keypairs.Length)
                    break;

                string[] parts = keypair.Split(new char[] { _NAME_VALUE_SEPARATOR }, 2, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length != 2)
                    throw new ArgumentException("Invalid parameter in the data", "tokenData");

                string key   = parts[0];
                string value = parts[1];

                _properties.TryAdd(key, value);
                keyPairCounter++;
            }

            if (!_properties.ContainsKey(_DEF_KEY) ||
                 _properties[_DEF_KEY] != _DEF_KEY_VAL)
                throw new ArgumentException("Invalid parameter in the data", "tokenData");
        }

        public override IEnumerable<string> GetDynamicMemberNames()
        {
            return _properties?.Keys?.ToArray() ?? new string[0];
        }

        public override bool TryGetIndex(GetIndexBinder binder, object[] indexes, out object result)
        {
            if (indexes == null || indexes.Length <= 0) 
                throw new IndexOutOfRangeException();

            if (indexes[0].GetType() != typeof(int) && indexes[0].GetType() != typeof(string))
                throw new ArgumentException("indexes");

            if (_properties == null)
                throw new InvalidOperationException("Current object internal state is not valid");

            var index = indexes[0];
            string key = null;
            if ( index is int)
            {
                int indexAsInt = (int)index;
                if (indexAsInt < 0 || indexAsInt >= _properties.Keys.Count)
                    throw new IndexOutOfRangeException();

                key = _properties.ElementAt(indexAsInt).Key;
            }
            else if ( index is string)
            {
                key = (string)index;

                if (_properties.ContainsKey(key) == false)
                    result = null; ;
            }

            if (string.IsNullOrEmpty(key))
                throw new ArgumentException("indexes");

            result = _properties[key];

            return true;
        }

        public override bool TryGetMember(GetMemberBinder binder, out object result)
        {
            if (string.IsNullOrEmpty(binder?.Name ?? string.Empty))
                throw new ArgumentException("binder.Name");

            if (_properties == null)
                throw new InvalidOperationException("Current object internal state is not valid");

            result = null;

            if (_properties.ContainsKey(binder.Name) )
            {
                string value = null;
                if (_properties.TryGetValue(binder.Name, out value))
                    result = value;
            }
            
            return true;
        }

        public override bool TrySetIndex(SetIndexBinder binder, object[] indexes, object value)
        {
            if (indexes == null || indexes.Length <= 0)
                throw new IndexOutOfRangeException();

            if (indexes[0].GetType() != typeof(int) && indexes[0].GetType() != typeof(string))
                throw new ArgumentException("indexes");

            if ((value is string) == false)
                throw new ArgumentException("value is not stirng");

            string sanifiedInput = this.SanifyInput((string)value);

            if (_properties == null)
                throw new InvalidOperationException("Current object internal state is not valid");

            var index = indexes[0];
            string key = null;
            if (index is int)
            {
                int indexAsInt = (int)index;
                if (indexAsInt < 0 || indexAsInt >= _properties.Keys.Count)
                    throw new IndexOutOfRangeException();

                key = _properties.ElementAt(indexAsInt).Key;
            }
            else if (index is string)
            {
                key = (string)index;

                if (_properties.ContainsKey(key) == false && !string.IsNullOrEmpty(key))
                    _properties.TryAdd(key,null);
            }

            if (string.IsNullOrEmpty(key))
                throw new ArgumentException("indexes");

            if (_SYS_KEYS.Contains(key)) //prevent change of unmodificable parameter
                return true;

            _properties[key] = sanifiedInput;

            return true;
        }

        public override bool TrySetMember(SetMemberBinder binder, object value)
        {
            if (string.IsNullOrEmpty(binder?.Name ?? string.Empty))
                throw new ArgumentException("binder.Name");

            if ((value is string) == false)
                throw new ArgumentException("value is not stirng");

            string sanifiedInput = this.SanifyInput((string)value);

            if (_properties == null)
                throw new InvalidOperationException("Current object internal state is not valid");

            if (_SYS_KEYS.Contains(binder.Name)) //prevent change of unmodificable parameter
                return true;

            if (_properties.ContainsKey(binder.Name) == false)
                _properties.TryAdd(binder.Name, sanifiedInput);
            else
                _properties[binder.Name] = sanifiedInput;

            return true;
        }

        public override string ToString()
        {
            string result = string.Empty;
            dynamic @this = this.ToDynamic;

            string sizeAsString = @this.__size.ToString();
            int size = (int)sizeAsString.FromInvariantString();

            StringBuilder builder = new StringBuilder();
            var keys = _properties?.Keys?.ToArray() ?? new string[0];
            foreach (string key in keys.OrderBy(k => k))
                builder.Append( key + _NAME_VALUE_SEPARATOR + _properties[key] + _ITEMS_SEPARATOR);

            if (builder.Length > size)
                throw new IndexOutOfRangeException($"Current data length is : {builder.Length}, but actual buffer expect maximum length of : {size}");

            if (builder.Length < size)
            {
                int paddingLength = size - builder.Length;
                builder.Append(this.GetRandomlyGenerateBase64String(paddingLength));
            }

            result = builder.ToString();
            return result;
        }

        /// <summary>
        /// Clone the current instance and get a new deep copy
        /// </summary>
        /// <returns></returns>
        public GenericToken Clone ()
        {
            return new GenericToken(_factoryRef, _properties);
        }

        /// <summary>
        /// Encrypt current token data to make it tamper proof and confidential
        /// </summary>
        /// <returns>A string representing the encrypted data inside the current token</returns>
        public string Encrypt()
        {
            return _factoryRef.Encrypt(this);
        }

        /// <summary>
        /// Check if the current token can be considered expired
        /// </summary>
        /// <param name="timeoutMs">Timeout in ms after which the curren token is considered expired (optional : if omitted token timeout is used)</param>
        /// <returns>A boolean value, true if the token is expired, false otherwhise</returns>
        public bool IsExpired (long timeoutMs = 0)
        {
            dynamic @this = this.ToDynamic;

            if (timeoutMs <= 0)
            {
                string tokenTimoutMsAsString = @this.__timeoutMs;
                long tokenTimeoutMs = tokenTimoutMsAsString.FromInvariantString();
                timeoutMs = tokenTimeoutMs > 0 ? tokenTimeoutMs : _DEF_EXPIRATION_MS;
            }

            string creationDateTimeTickAsString = @this.__creationTime;
            long creationDateTimeTicks = creationDateTimeTickAsString.FromInvariantString();

            DateTime creationDateTime = DateTime.MaxValue;
            if (creationDateTimeTicks > 0)
                creationDateTime = new DateTime(creationDateTimeTicks);

            return DateTime.Now.AddMilliseconds(-timeoutMs) > creationDateTime;
        }

        /// <summary>
        /// Check that the data stored are between the configured boundaries
        /// </summary>
        /// <returns>True if the data inserted are inside the boundaries, false otherwhise</returns>
        /// <remarks>For each field, field name and value will occupy space, but also separator characters.
        /// <para>{ Field1 = "value1" , Field2 = "value2" } will become : </para>
        /// <para>__␝standard␜Field1␝value1␜Field2␝value2␜xx..x</para></remarks>
        /// <para>As you can see there is a standard (not modifiable) field at the beginnging (few oher that are modifiable), your custom field, and optinal padding at the end. Everything separaterd by separator</para>
        public bool CheckBoundaries()
        {
            try
            {
                this.ToString();
                return true;
            }
            catch (Exception err)
            {
                return false;
            }
        }

        /// <summary>
        /// Generate a cryptographically secure string of chossen length
        /// </summary>
        /// <param name="length">The length of generated string</param>
        /// <returns>The cryptographically secoure string</returns>
        private string GetRandomlyGenerateBase64String(int length)
        {
            var random = new CryptoRandom();

            byte[] buffer = new byte[length];
            random.NextBytes(buffer);

            return Convert.ToBase64String(buffer).Substring(0,length);
        }

        /// <summary>
        /// Remove reserved character from input strings
        /// </summary>
        /// <param name="input">The input string to sanify</param>
        /// <returns>The sanified string</returns>
        private string SanifyInput (string input )
        {
            if (string.IsNullOrEmpty(input))
                input = string.Empty;

            return input.Replace(_ITEMS_SEPARATOR.ToString(), string.Empty)
                        .Replace(_NAME_VALUE_SEPARATOR.ToString(),string.Empty);
        }

    }
}
