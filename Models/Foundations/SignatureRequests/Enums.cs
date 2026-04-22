namespace DocuSignPoc.Models.Foundations.SignatureRequests;

public enum SignatureRequestStatus
{
    Draft = 0,
    Sent = 1,
    Delivered = 2,
    Completed = 3,   // signed
    Declined = 4,
    Voided = 5
}

public enum SignatureRequestType
{
    Email = 1,      // remote signing
    Embedded = 2    // onsite signing
}
