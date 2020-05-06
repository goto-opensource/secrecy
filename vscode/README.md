# Secrecy
## TLDR
Select a string, bring up `Quick fixes`, select a secret storage and let the magic happen.  
In the background the extension will put the secret in the configured location and generate a getter for it.
See settings. They are **workspace** specific.
For now the extension only generates the 'getters'. Set up the client and the project deps yourself - to help with this, see snippets: importhcv, importakv, etc. (examples at: https://github.com/LogMeIn/secret-usage-examples-guides)
This extension help You put secrets in secure storage solutions:
- AWS KMS (JavaScript/TypeScript, Java, C#, Python)
	- req: set up local AWS credentials
	- have AWS SDK/KMS in project deps - impkms snippet
- AWS SSM (JavaScript/TypeScript, Java, C#, Python)
	- req: set up local AWS credentials
	- have AWS SDK/KMS+SSM in project deps - impssm snippet
- Azure Key Vault (JavaScript/TypeScript, Java, C#, Python)
	- req: set up local Azure credentials, set up vault URL in config
	- have Azure SDK/Key Vault secrets in project deps - impakv snippet
- Hashicorp Vault (KV v1 support for: JavaScript/TypeScript, Python, C#, Java, Puppet)
	- req: setup Hashicorp Vault token (enable offer Hashicorp in config and it will ask for it, alternatively set up via ENV var) + HCV address+port in config
	- for puppet, set up Hiera to use HCV!
	- have the HCV SDK in project deps - imphcv snippet
- Credstash (JavaScript/TypeScript, Python, C#)
	- req: set up local AWS credentials
	- have credstash lib in project deps - impcredstash snippet
	- for C#: credstashbuilder snippet. Put it in a new file - or in an existing, I'm just a README.


---

## How to
### **Notice**
Select a string, bring up `Quick fixes`, select a secret storage and let the magic happen.  
In the background the extension will put the secret in the configured location and generate a getter for it.
See settings. They are **workspace** specific.
The extension only generates the 'getters'. Set up the client and the project deps yourself - to help with this, see snippets: importhcv, importakv, etc.. 
More and examples with specific guides at: https://github.com/LogMeIn/secret-usage-examples-guides


### AWS KMS, SSM
Follow this to set up AWS credentials locally:  
https://docs.aws.amazon.com/sdk-for-java/v1/developer-guide/setup-credentials.html  
The values given in config will be used for AWS region.  
If You didn't set up previously, now set up some AWS KMS keys to use for encryption. Give yourself and the app appropiate access. Same for SSM.

### Azure KV
The easiest way to set up Azure for local dev access is to download the Azure VSCode Extension pack and log in. The credentials provider will find it and use it for the local API calls.  
Also, make a Key Vault on the portal. Give Your user and the app appropiate permission on it (RW for yourself, RO for the app).  
Provide the URL in the config - remember, this is workspace specific!

### Credstash
Set up AWS credentials locally.  
The values given in config will be used for AWS region.  
If You didn't set up previously, set up credstash.  
Follow this: https://github.com/fugue/credstash

### Hashicorp Vault
Enable the offering of HCV to store the secret in config.  
Set up the vault itself, if You have no vault yet.  
Follow these:
* https://www.vaultproject.io/docs/install 
* https://www.vaultproject.io/docs/configuration
* https://www.vaultproject.io/docs/commands/server

Provide the token when asked - alternatively set it up in an ENV var called VAULT_TOKEN.  
Set up the Vault address and port. Be aware that if You use a self-signed cert, You will need to set this up too in the config.  