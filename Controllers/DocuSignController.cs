using System.Collections.Concurrent;
using System.Text.Json;
using System.Linq;
using System.Xml.Linq;
using DocuSignPoc.Models.Foundations.SignatureRequests;
using DocuSignPoc.Services.Foundations.DocuSign;
using DocuSignPoc.Services.Foundations.QuickBooks;
using DocuSignPoc.Services.Foundations.SignatureRequests;
using Microsoft.AspNetCore.Mvc;

namespace DocuSignPoc.Controllers;

[ApiController]
[Route("api/docusign")]
public class DocuSignController : ControllerBase
{
    private readonly IDocuSignService docuSignService;
    private readonly ISignatureRequestService signatureRequestService;
    private readonly IQuickBooksService quickBooksService;
    private readonly ILogger<DocuSignController> logger;
    
    // In-memory feed for local dashboard events (simplified)
    private static readonly ConcurrentBag<dynamic> events = new();

    public DocuSignController(
        IDocuSignService docuSignService, 
        ISignatureRequestService signatureRequestService,
        IQuickBooksService quickBooksService,
        ILogger<DocuSignController> logger)
    {
        this.docuSignService = docuSignService;
        this.signatureRequestService = signatureRequestService;
        this.quickBooksService = quickBooksService;
        this.logger = logger;
    }

    [HttpGet("events")]
    public IActionResult GetEvents() => Ok(events.OrderByDescending(e => e.timestamp));

    [HttpGet("requests")]
    public IActionResult GetAllRequests() => Ok(this.signatureRequestService.RetrieveAllSignatureRequests());

    [HttpPost("send-email")]
    public async Task<IActionResult> SendEmail([FromBody] SigningRequest request)
    {
        try
        {
            // 1. Create Model & Store in SQLite (DRAFT)
            var signatureRequest = new SignatureRequest {
                Id = Guid.NewGuid(),
                InvoiceId = request.InvoiceId,
                RecipientEmail = request.Email,
                RecipientName = request.Name,
                Type = SignatureRequestType.Email,
                Status = SignatureRequestStatus.Draft,
                CreatedDate = DateTimeOffset.UtcNow,
                UpdatedDate = DateTimeOffset.UtcNow,
                DocumentName = "Invoice Signature Request",
                CompanyName = request.CompanyName,
                AccountId = Guid.TryParse(request.CompanyId, out var cid) ? cid : Guid.Empty
            };
            await this.signatureRequestService.AddSignatureRequestAsync(signatureRequest);

            // 2. Trigger DocuSign
            string envelopeId = await this.docuSignService.SendEmailEnvelopeAsync(
                request.InvoiceId, request.Email, request.Name, request.CompanyId, request.CompanyName);

            // 3. Update Status (SENT)
            signatureRequest.EnvelopeId = envelopeId;
            signatureRequest.Status = SignatureRequestStatus.Sent;
            signatureRequest.SentDate = DateTimeOffset.UtcNow;
            await this.signatureRequestService.ModifySignatureRequestAsync(signatureRequest);

            AddLocalEvent("📧 Email Sent", $"Envelope {envelopeId} for {request.Email}");
            return Ok(signatureRequest);
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, "Error in SendEmail");
            return StatusCode(500, ex.Message);
        }
    }

    [HttpPost("embedded-signing-url")]
    public async Task<IActionResult> EmbeddedSigningUrl([FromBody] SigningRequest request)
    {
        try
        {
            // 1. Create Model & Store in SQLite (DRAFT)
            var signatureRequest = new SignatureRequest {
                Id = Guid.NewGuid(),
                InvoiceId = request.InvoiceId,
                RecipientEmail = request.Email,
                RecipientName = request.Name,
                Type = SignatureRequestType.Embedded,
                Status = SignatureRequestStatus.Draft,
                CreatedDate = DateTimeOffset.UtcNow,
                UpdatedDate = DateTimeOffset.UtcNow,
                DocumentName = "In-Person Signature Request",
                CompanyName = request.CompanyName,
                AccountId = Guid.TryParse(request.CompanyId, out var cid2) ? cid2 : Guid.Empty
            };
            await this.signatureRequestService.AddSignatureRequestAsync(signatureRequest);

            // 2. Trigger DocuSign
            string envelopeId = await this.docuSignService.CreateInPersonEnvelopeAsync(
                request.InvoiceId, request.Email, request.Name, request.CompanyId, request.CompanyName);

            string signingUrl = await this.docuSignService.GetInPersonSigningUrlAsync(
                envelopeId, request.Email, request.Name);

            // 3. Update Status (SENT)
            signatureRequest.EnvelopeId = envelopeId;
            signatureRequest.Status = SignatureRequestStatus.Sent;
            signatureRequest.SentDate = DateTimeOffset.UtcNow;
            await this.signatureRequestService.ModifySignatureRequestAsync(signatureRequest);

            AddLocalEvent("🖊️ In-Person Ready", $"Envelope {envelopeId} for {request.Email}");
            return Ok(new { signatureRequest, signingUrl });
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, "Error in EmbeddedSigningUrl");
            return StatusCode(500, ex.Message);
        }
    }

    [HttpGet("signed-callback")]
    public IActionResult SignedCallback([FromQuery] string envelopeId, [FromQuery] string eventStatus)
    {
        AddLocalEvent("🏁 Return", $"User returned from signing envelope {envelopeId}. Status: {eventStatus}");
        return Content($@"
            <html>
            <body style='font-family:sans-serif; background:#0f172a; color:#e2e8f0; display:flex; flex-direction:column; align-items:center; justify-content:center; height:100vh; margin:0;'>
                <div style='background:#1e293b; padding:40px; border-radius:20px; border:1px solid rgba(255,255,255,0.1); text-align:center;'>
                    <h1 style='color:#10b981;'>Redirecting...</h1>
                    <p style='color:#94a3b8;'>Signing session ended. Status: {eventStatus}</p>
                    <script>setTimeout(() => location.href='/', 2000);</script>
                </div>
            </body>
            </html>", "text/html");
    }

    [HttpGet("check-status/{envelopeId}")]
    public async Task<IActionResult> CheckStatus(string envelopeId)
    {
        try
        {
            string status = await this.docuSignService.GetStatusAsync(envelopeId);
            await ProcessStatusUpdate(envelopeId, status, "Polled");
            return Ok(new { envelopeId, status });
        }
        catch (Exception ex) { return StatusCode(500, ex.Message); }
    }

    [HttpPost("webhook")]
    public async Task<IActionResult> Webhook()
    {
        using var reader = new StreamReader(Request.Body);
        string body = await reader.ReadToEndAsync();
        try 
        {
            string? status = null, envelopeId = null;
            if (body.TrimStart().StartsWith("<")) {
                XDocument doc = XDocument.Parse(body);
                XNamespace ns = "http://www.docusign.net/dto/schemas/v2.1";
                var envelopeStatus = doc.Descendants(ns + "EnvelopeStatus").FirstOrDefault() ?? doc.Descendants("EnvelopeStatus").FirstOrDefault();
                status = envelopeStatus?.Element(ns + "Status")?.Value ?? envelopeStatus?.Element("Status")?.Value;
                envelopeId = envelopeStatus?.Element(ns + "EnvelopeID")?.Value ?? envelopeStatus?.Element("EnvelopeID")?.Value;
            } else {
                try {
                    var data = JsonDocument.Parse(body);
                    var root = data.RootElement;
                    if (root.TryGetProperty("status", out var sProp)) status = sProp.GetString();
                    else if (root.TryGetProperty("data", out var d) && d.TryGetProperty("status", out var ds)) status = ds.GetString();

                    if (root.TryGetProperty("envelopeId", out var eProp)) envelopeId = eProp.GetString();
                    else if (root.TryGetProperty("data", out var d2) && d2.TryGetProperty("envelopeId", out var de)) envelopeId = de.GetString();
                } catch { 
                    this.logger.LogError("Webhook JSON Parse Failed. Body: {Body}", body);
                }
            }
            
            if (string.IsNullOrEmpty(status) || string.IsNullOrEmpty(envelopeId)) {
                Console.WriteLine("\n[DEBUG] WEBOOK PARSE FAILED. Raw Body:\n" + body + "\n");
            }

            Console.WriteLine($"\n>>> WEBHOOK RECEIVED: Envelope={envelopeId}, Status={status} <<<\n");

            if (!string.IsNullOrEmpty(status) && !string.IsNullOrEmpty(envelopeId)) {
                await ProcessStatusUpdate(envelopeId, status, "Webhook");
            }
        } catch (Exception ex) { this.logger.LogError(ex, "Webhook processing error"); }
        return Ok();
    }

    private async Task ProcessStatusUpdate(string envelopeId, string status, string source)
    {
        var request = this.signatureRequestService.RetrieveAllSignatureRequests().FirstOrDefault(s => s.EnvelopeId == envelopeId);
        if (request == null) return;

        SignatureRequestStatus domainStatus = MapStatus(status);
        if (request.Status == domainStatus) return;

        request.Status = domainStatus;
        request.UpdatedDate = DateTimeOffset.UtcNow;
        
        Console.WriteLine($"\n>>> STATUS CHANGE: {envelopeId} is now {status} ({source}) <<<\n");

        if (domainStatus == SignatureRequestStatus.Completed) {
            request.SignedDate = DateTimeOffset.UtcNow;
            await this.quickBooksService.ProcessInvoiceAsync(request.InvoiceId);
            AddLocalEvent("✅ SIGNED (" + source + ")", $"Envelope {envelopeId} is COMPLETED.");
        } else {
            AddLocalEvent("📬 " + source + " Update", $"Envelope {envelopeId} status: {status}");
        }
        await this.signatureRequestService.ModifySignatureRequestAsync(request);
    }

    private SignatureRequestStatus MapStatus(string s) => (s?.ToLower()) switch {
        "sent" => SignatureRequestStatus.Sent, "delivered" => SignatureRequestStatus.Delivered,
        "completed" => SignatureRequestStatus.Completed, "declined" => SignatureRequestStatus.Declined,
        "voided" => SignatureRequestStatus.Voided, _ => SignatureRequestStatus.Draft
    };

    private static void AddLocalEvent(string title, string message) => events.Add(new { timestamp = DateTime.UtcNow.ToString("O"), title, message });
}

public record SigningRequest(string InvoiceId, string Email, string Name, string CompanyId, string CompanyName);
