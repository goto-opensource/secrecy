import { DefaultAzureCredential } from '@azure/identity'
import { SecretClient, KeyVaultSecret } from '@azure/keyvault-secrets'
import * as vscode from 'vscode';
const aws = require('aws-sdk')
const Credstash = require('nodecredstash');

let context: vscode.ExtensionContext;
let credstash: any;
let vault: any;
let kmsClient: any;
let ssmClient: any;
let kvClient: SecretClient;

const CREDSTASH_SUPPORTEDLANGUAGES = ["javascript", "typescript", "python", "csharp"];
const HASHICORP_SUPPORTEDLANGUAGES = ["javascript", "typescript", "python", "csharp", "java", "puppet"];
const AWSKMS_SUPPORTEDLANGUAGES = ["javascript", "typescript", "python", "csharp", "java"];
const AWSSSM_SUPPORTEDLANGUAGES = ["javascript", "typescript", "python", "csharp", "java"];
const AZUREKV_SUPPORTEDLANGUAGES = ["javascript", "typescript", "python", "csharp", "java"];

const PUTINCREDSTASHCOMMAND = "secrecy.putInCredstash";
const PUTINHASHICORPCOMMAND = "secrecy.putInHashicorp";
const ENCRYPTWITHKMSCOMMAND = "secrecy.encryptWithKMS";
const PUTINSSMCOMMAND = "secrecy.putInSSM";
const PUTINAZUREKVCOMMAND = "secrecy.putInAzureKV";
const RECREATE = "secrecy.recreate";
const REPLACEHCVTOKEN = "secrecy.replaceHCVToken";

export function activate(_context: vscode.ExtensionContext) {
	_context.subscriptions.push(
		vscode.languages.registerCodeActionsProvider('*', new Secrecy(), {
			providedCodeActionKinds: Secrecy.providedCodeActionKinds
		}));

	_context.subscriptions.push(
		vscode.commands.registerCommand(RECREATE, init),
		vscode.commands.registerCommand(REPLACEHCVTOKEN, setUpHCVToken),
		vscode.commands.registerCommand(PUTINCREDSTASHCOMMAND, Secrecy.putInCredStash),
		vscode.commands.registerCommand(PUTINHASHICORPCOMMAND, Secrecy.putInHashicorp),
		vscode.commands.registerCommand(ENCRYPTWITHKMSCOMMAND, Secrecy.encryptWithKMS),
		vscode.commands.registerCommand(PUTINSSMCOMMAND, Secrecy.putInSSM),
		vscode.commands.registerCommand(PUTINAZUREKVCOMMAND, Secrecy.putInAzureKV),
	);
	context = _context;
	init()
}

function init() {
	if (vscode.workspace.getConfiguration().get("secrecy.offerHashicorp") === true) {
		initVault();
	}
	if (vscode.workspace.getConfiguration().get("secrecy.offerCredstash") === true) {
		credstash = new Credstash({ awsOpts: { region: vscode.workspace.getConfiguration().get("secrecy.CredstashAWSRegion") } });
	}
	if (vscode.workspace.getConfiguration().get("secrecy.offerAWSKMS") === true) {
		aws.config.update({ region: vscode.workspace.getConfiguration().get("secrecy.AWSKMSRegion") });
		kmsClient = new aws.KMS();
	}
	if (vscode.workspace.getConfiguration().get("secrecy.offerAWSSSM") === true) {
		const aws = require('aws-sdk')
		aws.config.update({ region: vscode.workspace.getConfiguration().get("secrecy.AWSSSMRegion") });
		if (kmsClient === undefined)
			kmsClient = new aws.KMS();
		ssmClient = new aws.SSM();
	}
	if (vscode.workspace.getConfiguration().get("secrecy.offerAzureKV") === true) {
		const credential = new DefaultAzureCredential();
		const url = vscode.workspace.getConfiguration().get<string>("secrecy.AzureKeyVaultAdress", ""); // it has a default value so no worries. makes TS happy.
		kvClient = new SecretClient(url, credential);
	}
}

export class Secrecy implements vscode.CodeActionProvider {

	public static readonly providedCodeActionKinds = [
		vscode.CodeActionKind.QuickFix
	];

	public provideCodeActions(document: vscode.TextDocument, range: vscode.Range): vscode.CodeAction[] | undefined {
		if (range.isEmpty) return undefined;

		const ret: vscode.CodeAction[] = new Array<vscode.CodeAction>();
		if ((CREDSTASH_SUPPORTEDLANGUAGES.indexOf(document.languageId) > -1) && (vscode.workspace.getConfiguration().get("secrecy.offerCredstash") === true)) {
			const putInCredstash = this.createCredStashQuickFix(document, range);
			ret.push(putInCredstash);
		}
		if ((HASHICORP_SUPPORTEDLANGUAGES.indexOf(document.languageId) > -1) && (vscode.workspace.getConfiguration().get("secrecy.offerHashicorp") === true)) {
			const putInHashicorp = this.createHashicorpQuickFix(document, range);
			ret.push(putInHashicorp);
		}
		if ((AWSKMS_SUPPORTEDLANGUAGES.indexOf(document.languageId) > -1) && (vscode.workspace.getConfiguration().get("secrecy.offerAWSKMS") === true)) {
			const KMSQuickfix = this.createKMSQuickFix(document, range);
			ret.push(KMSQuickfix);
		}
		if ((AWSSSM_SUPPORTEDLANGUAGES.indexOf(document.languageId) > -1) && (vscode.workspace.getConfiguration().get("secrecy.offerAWSSSM") === true)) {
			const putInSSM = this.createSSMQuickFix(document, range);
			ret.push(putInSSM);
		}
		if ((AZUREKV_SUPPORTEDLANGUAGES.indexOf(document.languageId) > -1) && (vscode.workspace.getConfiguration().get("secrecy.offerAzureKV") === true)) {
			const putInAzureKV = this.createAzureKVQuickFix(document, range);
			ret.push(putInAzureKV);
		}
		return ret;
	}

	private createCredStashQuickFix(document: vscode.TextDocument, range: vscode.Range): vscode.CodeAction {
		const fix = new vscode.CodeAction(`Put this string in Credstash as a secret`, vscode.CodeActionKind.Refactor);
		fix.edit = new vscode.WorkspaceEdit();
		fix.command = { command: PUTINCREDSTASHCOMMAND, arguments: [document, range], title: "" };
		return fix;
	}

	private createHashicorpQuickFix(document: vscode.TextDocument, range: vscode.Range): vscode.CodeAction {
		const fix = new vscode.CodeAction(`Put this string in Hashicorp as a secret`, vscode.CodeActionKind.Refactor);
		fix.edit = new vscode.WorkspaceEdit();
		fix.command = { command: PUTINHASHICORPCOMMAND, arguments: [document, range], title: "" };
		return fix;
	}

	private createKMSQuickFix(document: vscode.TextDocument, range: vscode.Range): vscode.CodeAction {
		const fix = new vscode.CodeAction(`Encrypt this string with AWS KMS`, vscode.CodeActionKind.Refactor);
		fix.edit = new vscode.WorkspaceEdit();
		fix.command = { command: ENCRYPTWITHKMSCOMMAND, arguments: [document, range], title: "" };
		return fix;
	}

	private createSSMQuickFix(document: vscode.TextDocument, range: vscode.Range): vscode.CodeAction {
		const fix = new vscode.CodeAction(`Put this string in AWS SSM as an encrypted parameter`, vscode.CodeActionKind.Refactor);
		fix.edit = new vscode.WorkspaceEdit();
		fix.command = { command: PUTINSSMCOMMAND, arguments: [document, range], title: "" };
		return fix;
	}

	private createAzureKVQuickFix(document: vscode.TextDocument, range: vscode.Range): vscode.CodeAction {
		const fix = new vscode.CodeAction(`Put this secret in Azure Key Vault`, vscode.CodeActionKind.Refactor);
		fix.edit = new vscode.WorkspaceEdit();
		fix.command = { command: PUTINAZUREKVCOMMAND, arguments: [document, range], title: "" };
		return fix;
	}

	public static async putInHashicorp(doc: vscode.TextDocument, range: vscode.Range): Promise<void> {
		let value = doc.getText(range);
		const value_validated = await vscode.window.showInputBox({ prompt: "Validate the value (remove unnecessary \"\" etc.)!", value: value });
		if (value_validated === undefined) return;
		value = value_validated;
		let engine = await vscode.window.showInputBox({ prompt: "Provide the name of engine to store the secret!" });
		if (engine === undefined)
			return;
		let secret = await vscode.window.showInputBox({ prompt: "Provide the name of the secret!" });
		if (secret === undefined)
			return;
		let key: string | undefined;
		if (doc.languageId === "puppet") {
			key = vscode.workspace.getConfiguration().get("secrecy.hashicorpDefaultFieldForPuppet");
		} else
			key = await vscode.window.showInputBox({ prompt: "Provide the key in the secret!" });
		if (key === undefined)
			return;
		const obj: any = {};
		obj[key] = value;
		if (vault === undefined || vault.token === undefined) {
			await initVault();
		}
		vault.write(`${engine}/${secret}`, obj).then((value: any) => {
			const edit = new vscode.WorkspaceEdit();
			switch (doc.languageId) {
				case "javascript":
				case "typescript":
					edit.replace(doc.uri, new vscode.Range(range.start, range.end), `(await ${vscode.workspace.getConfiguration().get("secrecy.nameForHashicorpClient")}.read('${engine}/${secret}'))['data']['${key}']`);
					break;
				case "python":
					edit.replace(doc.uri, new vscode.Range(range.start, range.end), `${vscode.workspace.getConfiguration().get("secrecy.nameForHashicorpClient")}.secrets.kv.v1.read_secret(mount_point="${engine}", path='${secret}')['data']['${key}']`);
					break;
				case "csharp":
					edit.replace(doc.uri, new vscode.Range(range.start, range.end), `(await ${vscode.workspace.getConfiguration().get("secrecy.nameForHashicorpClient")}.Secret.Read<Dictionary<String, String>>("${engine}/${secret}")).Data["${key}"]`);
					break;
				case "java":
					edit.replace(doc.uri, new vscode.Range(range.start, range.end), `${vscode.workspace.getConfiguration().get("secrecy.nameForHashicorpClient")}.logical().read("${engine}/${secret}").getData().get("${key}")`);
					break;
				case "puppet":
					edit.replace(doc.uri, new vscode.Range(range.start, range.end), `lookup(${secret})`);
					break;
				default:
					break;
			}
			vscode.workspace.applyEdit(edit);
		}
		).catch((err: Error) => {
			console.error(err);
			vscode.window.showErrorMessage(err.message);
		});
	}

	public static async putInCredStash(doc: vscode.TextDocument, range: vscode.Range): Promise<void> {
		let value = doc.getText(range);
		const value_validated = await vscode.window.showInputBox({ prompt: "Validate the value (remove unnecessary \"\" etc.)!", value: value });
		if (value_validated === undefined) return;
		value = value_validated;
		let name = await vscode.window.showInputBox({ prompt: "Provide a name for the secret!" });
		if (name !== undefined && name !== "") {
			let version = Number.parseInt(await credstash.getHighestVersion({ name: name }));
			if (0 !== version) {
				const newname = await vscode.window.showInputBox({ prompt: "There is already a secret with this name! Provide another or press enter to override with new version!" });
				switch (newname) {
					case "":
						break;
					case undefined:
						return;
					default:
						name = newname;
						break;
				}
			}
			credstash.putSecret({ name: name, secret: value, version: version + 1 }).then(() => {
				const edit = new vscode.WorkspaceEdit();
				switch (doc.languageId) {
					case "javascript":
					case "typescript":
						edit.replace(doc.uri, new vscode.Range(range.start, range.end), `${!vscode.workspace.getConfiguration().get("secrecy.newClientForCredstash") ? "new Credstash({ awsOpts: { region: '" + vscode.workspace.getConfiguration().get("secrecy.CredstashAWSRegion") + "' } })" : vscode.workspace.getConfiguration().get("secrecy.nameForCredstashClient")}.getSecret({ name: '${name}', version: ${version + 1} })`);
						break;
					case "python":
						edit.replace(doc.uri, new vscode.Range(range.start, range.end), `${!vscode.workspace.getConfiguration().get("secrecy.newClientForCredstash") ? "credstash" : vscode.workspace.getConfiguration().get("secrecy.nameForCredstashClient")}.getSecret(name='${name}', version=${version + 1})`);
						break;
					case "csharp":
						edit.replace(doc.uri, new vscode.Range(range.start, range.end), `${!vscode.workspace.getConfiguration().get("secrecy.newClientForCredstash") ? `CredstashBuilder.WithRegion(Amazon.RegionEndpoint.GetBySystemName("${vscode.workspace.getConfiguration().get("secrecy.CredstashAWSRegion")}"))` : vscode.workspace.getConfiguration().get("secrecy.nameForCredstashClient")}.GetSecretAsync(name: "${name}", version: "${version + 1}" )`);
						break;
					default:
						break;
				}
				vscode.workspace.applyEdit(edit);
			}).catch((err: Error) => {
				console.error(err);
				vscode.window.showErrorMessage(err.message);
			});
		}
	}

	public static async encryptWithKMS(doc: vscode.TextDocument, range: vscode.Range): Promise<void> {
		let value = doc.getText(range);
		const value_validated = await vscode.window.showInputBox({ prompt: "Validate the value (remove unnecessary \"\" etc.)!", value: value });
		if (value_validated === undefined) return;
		value = value_validated;
		let keys: Array<{ AliasName: string, AliasArn: string }>;
		kmsClient.listAliases({}, async function (err: Error, data: { Aliases: Array<{ AliasName: string, AliasArn: string }> }) {
			if (err) {
				console.error(err);
				vscode.window.showErrorMessage(err.message);
				return;
			}
			else {
				keys = data.Aliases;
				const keyid = await vscode.window.showQuickPick(keys.map(e => e.AliasName), { placeHolder: "Choose a KMS key! The application needs to have Decrypt permissions with this key." });
				if (keyid === undefined) return;
				const buf = Buffer.from(value, 'utf8');
				var params = {
					KeyId: keyid,
					Plaintext: buf.toString('base64')
				};
				kmsClient.encrypt(params, function (err: Error, data: { CiphertextBlob: Buffer, KeyId: string, EncryptionAlgorithm: string }) {
					if (err) { vscode.window.showErrorMessage(err.message); return; }
					else {
						const edit = new vscode.WorkspaceEdit();
						switch (doc.languageId) {
							case "javascript":
							case "typescript":
								edit.replace(doc.uri, new vscode.Range(range.start, range.end), `(await ${!vscode.workspace.getConfiguration().get("secrecy.newClientForKMS") ? "new aws.KMS()" : vscode.workspace.getConfiguration().get("secrecy.nameForAWSKMSClient")}.decrypt({ CiphertextBlob: Buffer.from('${data.CiphertextBlob.toString('base64')}', 'base64') }).promise()).Plaintext.toString()`);
								break;
							case "python":
								edit.replace(doc.uri, new vscode.Range(range.start, range.end), `${!vscode.workspace.getConfiguration().get("secrecy.newClientForKMS") ? "boto3.client('kms', region_name='" + vscode.workspace.getConfiguration().get("secrecy.AWSSSMRegion") + "')" : vscode.workspace.getConfiguration().get("secrecy.nameForAWSKMSClient")}.decrypt(CiphertextBlob=base64.b64decode('${data.CiphertextBlob.toString('base64')}'))['Plaintext']`);
								break;
							case "csharp":
								edit.replace(doc.uri, new vscode.Range(range.start, range.end), `Encoding.ASCII.GetString((await ${!vscode.workspace.getConfiguration().get("secrecy.newClientForKMS") ? "new AmazonKeyManagementServiceClient()" : vscode.workspace.getConfiguration().get("secrecy.nameForAWSKMSClient")}.DecryptAsync(new DecryptRequest(){CiphertextBlob = new MemoryStream(System.Convert.FromBase64String("${data.CiphertextBlob.toString('base64')}"))})).Plaintext.ToArray())`);
								break;
							case "java":
								edit.replace(doc.uri, new vscode.Range(range.start, range.end), `new String(${!vscode.workspace.getConfiguration().get("secrecy.newClientForKMS") ? "AWSKMSClientBuilder.defaultClient()" : vscode.workspace.getConfiguration().get("secrecy.nameForAWSKMSClient")}.decrypt(new DecryptRequest().withCiphertextBlob(ByteBuffer.wrap(Base64.decode("${data.CiphertextBlob.toString('base64')}")))).getPlaintext().array())`);
								break;
							default:
								break;
						}
						vscode.workspace.applyEdit(edit);
					}
				});
			}
		});
	}

	public static async putInSSM(doc: vscode.TextDocument, range: vscode.Range): Promise<void> {
		let value = doc.getText(range);
		const value_validated = await vscode.window.showInputBox({ prompt: "Validate the value (remove unnecessary \"\" etc.)!", value: value });
		if (value_validated === undefined) return;
		value = value_validated;
		let name = await vscode.window.showInputBox({ prompt: "Provide a name for the secret parameter!" });
		if (name !== undefined && !range.isEmpty && name !== "") {
			var params = {
				Name: name, /* required */
				WithDecryption: false
			};
			ssmClient.getParameter(params, async function (err: Error, data: { Parameter: { Name: string, Version: number } }) {
				let version = 0;
				if (err) {
					/* ignore this */
				} else {
					version = data.Parameter.Version;
				}
				if (version >= 1) {
					const newname = await vscode.window.showInputBox({ prompt: "There is already a secret with this name! Provide another or press enter to override with new version!" });
					switch (newname) {
						case "":
							break;
						case undefined:
							return;
						default:
							name = newname;
							break;
					}
				}
				let keys: Array<{ AliasName: string, AliasArn: string }>;
				kmsClient.listAliases({}, async function (err: Error, data: { Aliases: Array<{ AliasName: string, AliasArn: string }> }) {
					if (err) {
						console.error(err);
						vscode.window.showErrorMessage(err.message);
						return;
					}
					else {
						keys = data.Aliases;
						const keyid = await vscode.window.showQuickPick(keys.map(e => e.AliasName), { placeHolder: "Choose a KMS key to encrypt the parameter! The application needs to have Decrypt permissions with this key." });
						if (keyid === undefined) return;
						var params = {
							Name: name,
							Type: "SecureString",
							Value: value,
							KeyId: keyid,
							Overwrite: true,
							Tier: "Standard"
						};
						ssmClient.putParameter(params, function (err: Error, data: { Version: number, Tier: string }) {
							if (err) {
								console.error(err);
								vscode.window.showErrorMessage(err.message);
								return;
							}
							else {
								const edit = new vscode.WorkspaceEdit();
								switch (doc.languageId) {
									case "javascript":
									case "typescript":
										edit.replace(doc.uri, new vscode.Range(range.start, range.end), `(await ${!vscode.workspace.getConfiguration().get("secrecy.newClientForSSM") ? "new aws.SSM()" : vscode.workspace.getConfiguration().get("secrecy.nameForAWSSSMClient")}.getParameter({ Name: '${name}', WithDecryption: true }).promise()).Parameter.Value`);
										break;
									case "python":
										edit.replace(doc.uri, new vscode.Range(range.start, range.end), `${!vscode.workspace.getConfiguration().get("secrecy.newClientForSSM") ? "boto3.client('ssm', region_name=" + vscode.workspace.getConfiguration().get("secrecy.AWSSSMRegion") + ")" : vscode.workspace.getConfiguration().get("secrecy.nameForAWSSSMClient")}.get_parameter(Name="${name}", WithDecryption=True)['Parameter']['Value']`);
										break;
									case "csharp":
										edit.replace(doc.uri, new vscode.Range(range.start, range.end), `(await ${!vscode.workspace.getConfiguration().get("secrecy.newClientForSSM") ? "new AmazonSimpleSystemsManagementClient()" : vscode.workspace.getConfiguration().get("secrecy.nameForAWSSSMClient")}.GetParameterAsync(new GetParameterRequest() { Name = "${name}", WithDecryption = true })).Parameter.Value`);
										break;
									case "java":
										edit.replace(doc.uri, new vscode.Range(range.start, range.end), `new String(${!vscode.workspace.getConfiguration().get("secrecy.newClientForSSM") ? "AWSSimpleSystemsManagementClientBuilder.defaultClient()" : !vscode.workspace.getConfiguration().get("secrecy.newClientForSSM") ? "new aws.SSM()" : vscode.workspace.getConfiguration().get("secrecy.nameForAWSSSMClient")}.getParameter(new GetParameterRequest().withName("${name}").withWithDecryption(true)).getParameter().getValue())`);
										break;
									default:
										break;
								}
								vscode.workspace.applyEdit(edit);
							}
						});
					}
				});
			});
		}
	}

	public static async putInAzureKV(doc: vscode.TextDocument, range: vscode.Range): Promise<void> {
		let value = doc.getText(range);
		const value_validated = await vscode.window.showInputBox({ prompt: "Validate the value (remove unnecessary \"\" etc.)!", value: value });
		if (value_validated === undefined) return;
		value = value_validated;
		let name = await vscode.window.showInputBox({ prompt: "Provide a name for the secret!" });
		if (name !== undefined && name !== "") {
			let existing: KeyVaultSecret | undefined = undefined;
			try {
				existing = await kvClient.getSecret(name);
			} catch {
			}
			if (existing !== undefined) {
				const newname = await vscode.window.showInputBox({ prompt: "There is already a secret with this name! Provide another or press enter to override with new version!" });
				switch (newname) {
					case "":
						break;
					case undefined:
						return;
					default:
						name = newname;
						break;
				}
			}
			kvClient.setSecret(name, value).then((value: KeyVaultSecret) => {

				const edit = new vscode.WorkspaceEdit();
				switch (doc.languageId) {
					case "javascript":
					case "typescript":
						edit.replace(doc.uri, new vscode.Range(range.start, range.end), `(await ${!vscode.workspace.getConfiguration().get("secrecy.newClientForAKV") ? "new SecretClient('" + vscode.workspace.getConfiguration().get<string>("secrecy.AzureKeyVaultAdress", "") + "', new ManagedIdentityCredential())" : vscode.workspace.getConfiguration().get("secrecy.nameForAzureKVClient")}.getSecret('${name}')).value`);
						break;
					case "python":
						edit.replace(doc.uri, new vscode.Range(range.start, range.end), `${!vscode.workspace.getConfiguration().get("secrecy.newClientForAKV") ? "SecretClient(vault_url='" + vscode.workspace.getConfiguration().get<string>("secrecy.AzureKeyVaultAdress", "") + "', credential=DefaultAzureCredential())" : vscode.workspace.getConfiguration().get("secrecy.nameForAzureKVClient")}.get_secret("${name}").value`);
						break;
					case "csharp":
						edit.replace(doc.uri, new vscode.Range(range.start, range.end), `(await ${!vscode.workspace.getConfiguration().get("secrecy.newClientForAKV") ? "new KeyVaultClient(new KeyVaultClient.AuthenticationCallback(new AzureServiceTokenProvider().KeyVaultTokenCallback))" : vscode.workspace.getConfiguration().get("secrecy.nameForAzureKVClient")}.GetSecretAsync("${vscode.workspace.getConfiguration().get("secrecy.AzureKeyVaultAdress")}/secrets/${name}")).Value`);
						break;
					case "java":
						edit.replace(doc.uri, new vscode.Range(range.start, range.end), `${!vscode.workspace.getConfiguration().get("secrecy.newClientForAKV") ? "new SecretClientBuilder().vaultUrl(\"" + vscode.workspace.getConfiguration().get<string>("secrecy.AzureKeyVaultAdress", "") + "\").credential(new DefaultAzureCredentialBuilder().build()).buildClient()" : vscode.workspace.getConfiguration().get("secrecy.nameForAzureKVClient")}.getSecret("${name}").getValue()`);
						break;
					default:
						break;
				}
				vscode.workspace.applyEdit(edit);
			}).catch((err: Error) => {
				console.error(err);
				vscode.window.showErrorMessage(err.message);
				return;
			});
		}
	}
}

async function initVault() {
	if (context.workspaceState.get("hashicorp_token") === undefined || context.workspaceState.get("hashicorp_token") === "") {
		if (process.env.VAULT_TOKEN === undefined || process.env.VAULT_TOKEN === "") {
			await vscode.window.showInputBox({ prompt: "Provide Hashicorp token to use. This will be workspace specific." }).then(value => context.workspaceState.update("hashicorp_token", value));
		}
		else {
			context.workspaceState.update("hashicorp_token", process.env.VAULT_TOKEN);
		}
	}
	var options =
	{
		apiVersion: 'v1',
		endpoint: `${vscode.workspace.getConfiguration().get("secrecy.HashicorpAddress")}:${vscode.workspace.getConfiguration().get("secrecy.HashicorpPort")}`,
		token: context.workspaceState.get("hashicorp_token")
	};
	process.env.VAULT_SKIP_VERIFY = vscode.workspace.getConfiguration().get("secrecy.HashicorpSelfsigned");
	vault = require("node-vault")(options);
}

async function setUpHCVToken() {
	await vscode.window.showInputBox({ prompt: "Provide Hashicorp token to use. This will be workspace specific." }).then(value => context.workspaceState.update("hashicorp_token", value));
	await initVault();
}