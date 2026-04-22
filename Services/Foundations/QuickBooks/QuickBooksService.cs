using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace DocuSignPoc.Services.Foundations.QuickBooks;

public class QuickBooksService : IQuickBooksService
{
    private readonly ILogger<QuickBooksService> logger;

    public QuickBooksService(ILogger<QuickBooksService> logger)
    {
        this.logger = logger;
    }

    public async ValueTask ProcessInvoiceAsync(string invoiceId)
    {
        this.logger.LogInformation(">>> [QUICKBOOKS] Processing Invoice {InvoiceId} <<<", invoiceId);
        
        // Mock processing delay
        await Task.Delay(100);

        this.logger.LogInformation(">>> [QUICKBOOKS] Invoice {InvoiceId} Processed Successfully! <<<", invoiceId);
    }
}
