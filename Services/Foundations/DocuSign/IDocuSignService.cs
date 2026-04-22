using DocuSign.eSign.Model;

namespace DocuSignPoc.Services.Foundations.DocuSign;

public interface IDocuSignService
{
    Task<string> SendEmailEnvelopeAsync(string invoiceId, string email, string name, string companyId, string companyName);
    Task<string> CreateInPersonEnvelopeAsync(string invoiceId, string email, string name, string companyId, string companyName);
    Task<string> GetInPersonSigningUrlAsync(string envelopeId, string email, string name);
    Task<string> GetStatusAsync(string envelopeId);
}
