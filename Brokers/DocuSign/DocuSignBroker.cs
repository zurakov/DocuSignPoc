using DocuSign.eSign.Api;
using DocuSign.eSign.Client;
using DocuSign.eSign.Client.Auth;
using DocuSign.eSign.Model;

namespace DocuSignPoc.Brokers.DocuSign;

public interface IDocuSignBroker
{
    Task<string> CreateEnvelopeAsync(EnvelopeDefinition envelopeDefinition);
    Task<string> GetEmbeddedSigningUrlAsync(string envelopeId, RecipientViewRequest viewRequest);
    Task<Envelope> GetEnvelopeAsync(string envelopeId);
}

public class DocuSignBroker : IDocuSignBroker
{
    private readonly IConfiguration configuration;
    private readonly DocuSignClient docuSignClient;
    private string? accessToken;
    private DateTime accessTokenExpiry;

    public DocuSignBroker(IConfiguration configuration)
    {
        this.configuration = configuration;
        this.docuSignClient = new DocuSignClient(configuration["DocuSign:BaseUrl"]);
    }

    private async Task AuthenticateAsync()
    {
        if (this.accessToken != null && DateTime.UtcNow < this.accessTokenExpiry)
            return;

        byte[] privateKeyBytes = File.ReadAllBytes(this.configuration["DocuSign:PrivateKeyPath"]!);

        var token = await this.docuSignClient.RequestJWTUserTokenAsync(
            this.configuration["DocuSign:IntegrationKey"],
            this.configuration["DocuSign:UserId"],
            "account-d.docusign.com", // demo
            privateKeyBytes,
            1,
            new List<string> { "signature", "impersonation" });

        this.accessToken = token.access_token;
        this.accessTokenExpiry = DateTime.UtcNow.AddSeconds((double)(token.expires_in ?? 3600) - 60);
        this.docuSignClient.Configuration.DefaultHeader["Authorization"] = $"Bearer {this.accessToken}";
    }

    public async Task<string> CreateEnvelopeAsync(EnvelopeDefinition envelopeDefinition)
    {
        await AuthenticateAsync();
        var envelopesApi = new EnvelopesApi(this.docuSignClient);
        
        EnvelopeSummary summary = await envelopesApi.CreateEnvelopeAsync(
            this.configuration["DocuSign:AccountId"], 
            envelopeDefinition);

        return summary.EnvelopeId;
    }

    public async Task<string> GetEmbeddedSigningUrlAsync(string envelopeId, RecipientViewRequest viewRequest)
    {
        await AuthenticateAsync();
        var envelopesApi = new EnvelopesApi(this.docuSignClient);

        ViewUrl viewUrl = await envelopesApi.CreateRecipientViewAsync(
            this.configuration["DocuSign:AccountId"], 
            envelopeId, 
            viewRequest);

        return viewUrl.Url;
    }

    public async Task<Envelope> GetEnvelopeAsync(string envelopeId)
    {
        await AuthenticateAsync();
        var envelopesApi = new EnvelopesApi(this.docuSignClient);

        return await envelopesApi.GetEnvelopeAsync(
            this.configuration["DocuSign:AccountId"], 
            envelopeId);
    }
}
