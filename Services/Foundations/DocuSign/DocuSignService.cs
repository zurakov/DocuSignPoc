using DocuSign.eSign.Model;
using DocuSignPoc.Brokers.DocuSign;

namespace DocuSignPoc.Services.Foundations.DocuSign;

public class DocuSignService : IDocuSignService
{
    private readonly IDocuSignBroker docuSignBroker;
    private readonly IConfiguration configuration;
    private readonly ILogger<DocuSignService> logger;

    public DocuSignService(
        IDocuSignBroker docuSignBroker, 
        IConfiguration configuration,
        ILogger<DocuSignService> logger)
    {
        this.docuSignBroker = docuSignBroker;
        this.configuration = configuration;
        this.logger = logger;
    }

    public async Task<string> SendEmailEnvelopeAsync(string invoiceId, string email, string name, string companyId, string companyName)
    {
        EnvelopeDefinition envelopeDefinition = CreateBaseEnvelope(invoiceId, email, name, companyId, companyName);
        
        envelopeDefinition.Recipients = new Recipients
        {
            Signers = new List<Signer>
            {
                new Signer
                {
                    Email = email,
                    Name = name,
                    RecipientId = "1",
                    RoutingOrder = "1",
                    Tabs = CreateTabs()
                }
            }
        };

        return await this.docuSignBroker.CreateEnvelopeAsync(envelopeDefinition);
    }

    public async Task<string> CreateInPersonEnvelopeAsync(string invoiceId, string email, string name, string companyId, string companyName)
    {
        string hostEmail = this.configuration["DocuSign:HostEmail"] ?? "zafarurakov@outlook.com";
        string hostName = this.configuration["DocuSign:HostName"] ?? "Zafar Urakov";

        EnvelopeDefinition envelopeDefinition = CreateBaseEnvelope(invoiceId, email, name, companyId, companyName);
        envelopeDefinition.EmailSubject = "In-Person Signing Session";

        envelopeDefinition.Recipients = new Recipients
        {
            InPersonSigners = new List<InPersonSigner>
            {
                new InPersonSigner
                {
                    HostEmail = hostEmail,
                    HostName = hostName,
                    SignerEmail = email,
                    SignerName = name,
                    RecipientId = "1",
                    RoutingOrder = "1",
                    ClientUserId = email, // For embedded
                    Tabs = CreateTabs()
                }
            }
        };

        return await this.docuSignBroker.CreateEnvelopeAsync(envelopeDefinition);
    }

    public async Task<string> GetInPersonSigningUrlAsync(string envelopeId, string email, string name)
    {
        string hostEmail = this.configuration["DocuSign:HostEmail"] ?? "zafarurakov@outlook.com";
        string hostName = this.configuration["DocuSign:HostName"] ?? "Zafar Urakov";

        var viewRequest = new RecipientViewRequest
        {
            ReturnUrl = this.configuration["DocuSign:ReturnUrl"] + $"?envelopeId={envelopeId}&event=signing_complete",
            AuthenticationMethod = "none",
            Email = hostEmail,
            UserName = hostName,
            ClientUserId = email // Must match ClientUserId in EnvelopeDefinition
        };

        return await this.docuSignBroker.GetEmbeddedSigningUrlAsync(envelopeId, viewRequest);
    }

    public async Task<string> GetStatusAsync(string envelopeId)
    {
        Envelope envelope = await this.docuSignBroker.GetEnvelopeAsync(envelopeId);
        return envelope.Status;
    }

    private EnvelopeDefinition CreateBaseEnvelope(string invoiceId, string email, string name, string companyId, string companyName)
    {
        byte[] pdfBytes = File.ReadAllBytes("samples/invoice.pdf");

        return new EnvelopeDefinition
        {
            EmailSubject = $"Invoice {invoiceId} for {name} ({companyName})",
            Documents = new List<Document>
            {
                new Document
                {
                    DocumentBase64 = Convert.ToBase64String(pdfBytes),
                    Name = "Invoice",
                    FileExtension = "pdf",
                    DocumentId = "1"
                }
            },
            CustomFields = new CustomFields
            {
                TextCustomFields = new List<TextCustomField>
                {
                    new() { Name = "companyId",   Value = companyId,   Required = "true", Show = "false" },
                    new() { Name = "companyName", Value = companyName, Required = "true", Show = "false" },
                    new() { Name = "invoiceId",   Value = invoiceId,   Required = "true", Show = "false" }
                }
            },
            Status = "sent",
            EventNotification = CreateEventNotification()
        };
    }

    private EventNotification? CreateEventNotification()
    {
        string? url = this.configuration["DocuSign:WebhookUrl"];

        // DocuSign requires a real, public HTTPS URL for Connect notifications.
        // We skip adding it if it's missing, is HTTP, or still contains the placeholder.
        if (string.IsNullOrWhiteSpace(url) || 
            !url.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ||
            url.Contains("<"))
        {
            this.logger.LogWarning("DocuSign Webhook skipped: WebhookUrl is unconfigured or not HTTPS ({Url})", url ?? "null");
            return null;
        }

        this.logger.LogInformation("Attaching EventNotification (Webhook) with URL: {Url}", url);

        // Reliability fix: Per-envelope webhook (EventNotification)
        // Uses JSON (SIM - Send Individual Messages) for robust delivery
        return new EventNotification
        {
            Url = url,
            LoggingEnabled = "true",
            DeliveryMode = "SIM",
            EnvelopeEvents = new List<EnvelopeEvent>
            {
                new EnvelopeEvent { EnvelopeEventStatusCode = "delivered" },
                new EnvelopeEvent { EnvelopeEventStatusCode = "completed" },
                new EnvelopeEvent { EnvelopeEventStatusCode = "declined" },
                new EnvelopeEvent { EnvelopeEventStatusCode = "voided" }
            },
            IncludeCertificateOfCompletion = "false",
            IncludeDocuments = "false",
            IncludeEnvelopeVoidReason = "true",
            IncludeSenderAccountAsCustomField = "true",
            IncludeTimeZone = "true"
        };
    }

    private Tabs CreateTabs()
    {
        return new Tabs
        {
            SignHereTabs = new List<SignHere>
            {
                new SignHere
                {
                    DocumentId = "1",
                    PageNumber = "1",
                    RecipientId = "1",
                    TabLabel = "SignHere",
                    XPosition = "200",
                    YPosition = "600"
                }
            }
        };
    }
}
