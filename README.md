# Token Generator
## A simple tool to generate Tokens

TokenGenerator is a simple project that aim to provide a easy way to gnerate Tokens.
Usually token are random sequences, and then they are bound to user/informations throught a DataBase, but such scenario required a more complicated infrastructure, and need to handle the lifecycle of the token.
The approach followd by this library is different, here tokens are more similar to an encrypted cookie, and sensitive informations are stored directly in the token (in an encrypted unmodificable way).

With this approach you can distribute tokens among your users, and tokens still can be used to veicolate information among your appications, with a simpler architecture.
By contrast revoking a token may be trikier with the approach followed, that at the momonet require a master key to be shared among the application that will support the token

#### Usage Example

```csharp
        static void Main(string[] args)
        {
            SecureString masterKey = new SecureString();
            masterKey.AppendChar('M');
            masterKey.AppendChar('a');
            masterKey.AppendChar('s');
            masterKey.AppendChar('t');
            masterKey.AppendChar('e');
            masterKey.AppendChar('r');
            masterKey.AppendChar('K');
            masterKey.AppendChar('e');
            masterKey.AppendChar('y');

            //Setup the facotry with a specific master key (opionally setting up token default timout and data size) 
            GenericTokenFactory.Setup(masterKey, tokenSize : 150, timeoutMs: 20_000);

            //After setup we can obtain a factory instance
            var factory = GenericTokenFactory.Instance();

            //With the factory we can build as many token as reuquired
            var token1 = factory.Create();
            var token2 = factory.Create();

            //foreach token we can attach information, infomration must be strings
            var @dynToken1 = token1.ToDynamic;
            @dynToken1["UserName"] = "CurrentUser";
            @dynToken1.OtherData = "OtherData";
            @dynToken1.DecimalValue = 3.44567.AsInvariantString();

            //check if data persisted in the token fit the token size (token have predefined fixed size to not disclose any hint about the content)
            Console.WriteLine("Are boundaries valid : " + token1.CheckBoundaries().ToString());

            //unencrypted token representation
            Console.WriteLine("Plaintext of token1       : " + token1.ToString());

            //deep clone a token from another
            var token1Clone = token1.Clone();
            token1Clone.ToDynamic.OtherData = "NewValue";

            //unencrypted token representation
            string unencryptedTokenData = token1Clone.ToString();
            Console.WriteLine("Plaintext of token1 clone : " + unencryptedTokenData);

            //encrypt token data before distributing it
            string encryptedToken = token1.Encrypt();
            Console.WriteLine("Encrypted token : " + encryptedToken + "\r\n");

            //get a token back when encrypted (when received back after distribution)
            var decryptedToken = factory.Decrypt(encryptedToken);

            //get back a token from its string representation in plaintext
            var token = factory.Create(unencryptedTokenData); //similar to colne operation, usefull if token was persisted in plaintext before distribution (do not use it for cloning pourpose)

            //check if the token exist time has exceeded timout, check is made against a custom timeout supplied of one minute.
            //if no data is supplied to the method the timeout of reference is the value passed in the setup phase (20s)
            Console.WriteLine("Is expired : " + token.IsExpired(timeoutMs: 60_000).ToString());

            //try to istantiate a token from invalid data 
            Console.WriteLine("Try to instantiate Token from invalid data");//plaintext is used, same happened from encrypted text if invalid
            try
            {
                var invalidToken = factory.Create("Some invalid, not token like, data");
            }
            catch (Exception err )
            {
                Console.WriteLine("Creation failed : " + err.Message);
            }

            Console.WriteLine("");
            Console.WriteLine("Type a key to contienue..");
            Console.ReadKey();
        }
```
## License

MIT
