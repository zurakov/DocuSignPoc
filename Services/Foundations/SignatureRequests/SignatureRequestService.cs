using System;
using System.Linq;
using System.Threading.Tasks;
using DocuSignPoc.Brokers.Storages;
using DocuSignPoc.Models.Foundations.SignatureRequests;

namespace DocuSignPoc.Services.Foundations.SignatureRequests;

public class SignatureRequestService : ISignatureRequestService
{
    private readonly IStorageBroker storageBroker;

    public SignatureRequestService(IStorageBroker storageBroker)
    {
        this.storageBroker = storageBroker;
    }

    public async ValueTask<SignatureRequest> AddSignatureRequestAsync(SignatureRequest signatureRequest)
    {
        return await this.storageBroker.InsertSignatureRequestAsync(signatureRequest);
    }

    public IQueryable<SignatureRequest> RetrieveAllSignatureRequests()
    {
        return this.storageBroker.SelectAllSignatureRequests();
    }

    public async ValueTask<SignatureRequest> ModifySignatureRequestAsync(SignatureRequest signatureRequest)
    {
        return await this.storageBroker.UpdateSignatureRequestAsync(signatureRequest);
    }

    public async ValueTask<SignatureRequest> RemoveSignatureRequestByIdAsync(Guid signatureRequestId)
    {
        SignatureRequest signatureRequest = this.storageBroker.SelectAllSignatureRequests()
            .FirstOrDefault(s => s.Id == signatureRequestId);
        
        return await this.storageBroker.DeleteSignatureRequestAsync(signatureRequest);
    }
}
