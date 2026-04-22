using System.Linq;
using System.Threading.Tasks;
using DocuSignPoc.Models.Foundations.SignatureRequests;

namespace DocuSignPoc.Services.Foundations.SignatureRequests;

public interface ISignatureRequestService
{
    ValueTask<SignatureRequest> AddSignatureRequestAsync(SignatureRequest signatureRequest);
    IQueryable<SignatureRequest> RetrieveAllSignatureRequests();
    ValueTask<SignatureRequest> ModifySignatureRequestAsync(SignatureRequest signatureRequest);
    ValueTask<SignatureRequest> RemoveSignatureRequestByIdAsync(Guid signatureRequestId);
}
