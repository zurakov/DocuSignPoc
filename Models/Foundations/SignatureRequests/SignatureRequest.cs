using System;

namespace DocuSignPoc.Models.Foundations.SignatureRequests;

public class SignatureRequest
{
    public Guid Id { get; set; }
    public Guid AccountId { get; set; }
    public string? InvoiceId { get; set; }
    public string? EnvelopeId { get; set; }
    public SignatureRequestType Type { get; set; }
    public SignatureRequestStatus Status { get; set; }
    public DateTimeOffset CreatedDate { get; set; }
    public DateTimeOffset UpdatedDate { get; set; }
    public DateTimeOffset? SentDate { get; set; }
    public DateTimeOffset? SignedDate { get; set; }
    public string RecipientEmail { get; set; } = default!;
    public string RecipientName { get; set; } = default!;
    public string DocumentName { get; set; } = default!;
    public string CompanyName { get; set; } = default!;
}
