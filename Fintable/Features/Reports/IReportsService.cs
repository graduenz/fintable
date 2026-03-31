namespace Fintable.Features.Reports
{
    public interface IReportsService
    {
        Task<StatsReportDto> GetStatsReportAsync();
        Task<FintableReportDto> GetFintableReportAsync(int year);
    }
}
