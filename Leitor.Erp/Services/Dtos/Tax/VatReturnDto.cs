using System;

namespace Leitor.Erp.Services.Dtos.Tax;

public class VatReturnDto
{
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public decimal OutputVat { get; set; }
    public decimal InputVat { get; set; }
    public decimal NetVatPayable { get; set; }
}
