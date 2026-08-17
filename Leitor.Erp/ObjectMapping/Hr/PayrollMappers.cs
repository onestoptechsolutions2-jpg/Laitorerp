using Leitor.Erp.Entities.Hr;
using Leitor.Erp.Services.Dtos.Hr;
using Riok.Mapperly.Abstractions;
using Volo.Abp.Mapperly;

namespace Leitor.Erp.ObjectMapping.Hr;

[Mapper]
public partial class PayeTaxBandToPayeTaxBandDtoMapper : MapperBase<PayeTaxBand, PayeTaxBandDto>
{
    [MapperIgnoreSource(nameof(PayeTaxBand.ExtraProperties))]
    [MapperIgnoreSource(nameof(PayeTaxBand.ConcurrencyStamp))]
    public override partial PayeTaxBandDto Map(PayeTaxBand source);

    [MapperIgnoreSource(nameof(PayeTaxBand.ExtraProperties))]
    [MapperIgnoreSource(nameof(PayeTaxBand.ConcurrencyStamp))]
    public override partial void Map(PayeTaxBand source, PayeTaxBandDto destination);
}

[Mapper]
public partial class NssfTierToNssfTierDtoMapper : MapperBase<NssfTier, NssfTierDto>
{
    [MapperIgnoreSource(nameof(NssfTier.ExtraProperties))]
    [MapperIgnoreSource(nameof(NssfTier.ConcurrencyStamp))]
    public override partial NssfTierDto Map(NssfTier source);

    [MapperIgnoreSource(nameof(NssfTier.ExtraProperties))]
    [MapperIgnoreSource(nameof(NssfTier.ConcurrencyStamp))]
    public override partial void Map(NssfTier source, NssfTierDto destination);
}

[Mapper]
public partial class PayrollRunToPayrollRunDtoMapper : MapperBase<PayrollRun, PayrollRunDto>
{
    [MapperIgnoreSource(nameof(PayrollRun.ExtraProperties))]
    [MapperIgnoreSource(nameof(PayrollRun.ConcurrencyStamp))]
    [MapperIgnoreTarget(nameof(PayrollRunDto.RunByUserName))]
    [MapperIgnoreTarget(nameof(PayrollRunDto.TotalNetPay))]
    [MapperIgnoreTarget(nameof(PayrollRunDto.EmployeeCount))]
    [MapperIgnoreTarget(nameof(PayrollRunDto.Lines))]
    public override partial PayrollRunDto Map(PayrollRun source);

    [MapperIgnoreSource(nameof(PayrollRun.ExtraProperties))]
    [MapperIgnoreSource(nameof(PayrollRun.ConcurrencyStamp))]
    [MapperIgnoreTarget(nameof(PayrollRunDto.RunByUserName))]
    [MapperIgnoreTarget(nameof(PayrollRunDto.TotalNetPay))]
    [MapperIgnoreTarget(nameof(PayrollRunDto.EmployeeCount))]
    [MapperIgnoreTarget(nameof(PayrollRunDto.Lines))]
    public override partial void Map(PayrollRun source, PayrollRunDto destination);
}

[Mapper]
public partial class PayrollRunLineToPayrollRunLineDtoMapper : MapperBase<PayrollRunLine, PayrollRunLineDto>
{
    [MapperIgnoreSource(nameof(PayrollRunLine.ExtraProperties))]
    [MapperIgnoreSource(nameof(PayrollRunLine.ConcurrencyStamp))]
    [MapperIgnoreTarget(nameof(PayrollRunLineDto.EmployeeName))]
    public override partial PayrollRunLineDto Map(PayrollRunLine source);

    [MapperIgnoreSource(nameof(PayrollRunLine.ExtraProperties))]
    [MapperIgnoreSource(nameof(PayrollRunLine.ConcurrencyStamp))]
    [MapperIgnoreTarget(nameof(PayrollRunLineDto.EmployeeName))]
    public override partial void Map(PayrollRunLine source, PayrollRunLineDto destination);
}
