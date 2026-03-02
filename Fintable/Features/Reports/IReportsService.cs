namespace Fintable.Features.Reports
{
    public interface IReportsService
    {
        Task<StatsReportDto> GetStatsReportAsync();
    }
}
