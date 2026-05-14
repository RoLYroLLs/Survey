using System.ComponentModel.DataAnnotations;

namespace Survey.Application.Models;

public class AreaListItem
{
	public int Id { get; set; }
	public string Name { get; set; } = string.Empty;
	public string? Description { get; set; }
	public int CountyCount { get; set; }
	public int GoalCount { get; set; }
	public DateTimeOffset UpdatedUtc { get; set; }
	public string? CountyNameFilter { get; set; }
}

public class AreaEditModel
{
	public int? Id { get; set; }

	[Required]
	[StringLength(200)]
	public string Name { get; set; } = string.Empty;

	[StringLength(2000)]
	public string? Description { get; set; }

	public List<string> SelectedCountyFips { get; set; } = [];
	public IReadOnlyList<CountyOptionItem> CountyOptions { get; set; } = Array.Empty<CountyOptionItem>();
}

public class CountyOptionItem
{
	public string CountyFips { get; set; } = string.Empty;
	public string CountyName { get; set; } = string.Empty;
	public string StateCode { get; set; } = string.Empty;
	public int ZipCount { get; set; }
}

public class ZipCountyMappingListItem
{
	public int Id { get; set; }
	public string ZipCode { get; set; } = string.Empty;
	public string CountyFips { get; set; } = string.Empty;
	public string CountyName { get; set; } = string.Empty;
	public string StateCode { get; set; } = string.Empty;
	public decimal ResidentialRatio { get; set; }
}

public class ZipCountyMappingEditModel
{
	public int? Id { get; set; }

	[Required]
	[RegularExpression(@"^\d{5}(-\d{4})?$", ErrorMessage = "Enter a valid ZIP code.")]
	public string ZipCode { get; set; } = string.Empty;

	[Required]
	[StringLength(5)]
	public string CountyFips { get; set; } = string.Empty;

	[Required]
	[StringLength(200)]
	public string CountyName { get; set; } = string.Empty;

	[Required]
	[StringLength(2)]
	public string StateCode { get; set; } = string.Empty;

	[Range(typeof(decimal), "0", "1")]
	public decimal ResidentialRatio { get; set; }
}

public class ZipCountyImportModel
{
	[Required]
	public string CsvContent { get; set; } = string.Empty;

	public bool ReplaceExisting { get; set; } = true;
}

public class ZipCountyImportResultModel
{
	public int ImportedRowCount { get; set; }
	public int DistinctZipCount { get; set; }
	public int DistinctCountyCount { get; set; }
}

public class GoalListItem
{
	public int Id { get; set; }
	public string Name { get; set; } = string.Empty;
	public string? AreaName { get; set; }
	public string? SurveyName { get; set; }
	public int TargetResponseCount { get; set; }
	public int CompletedResponses { get; set; }
	public int RemainingResponses { get; set; }
	public decimal ProgressPercent { get; set; }
	public DateOnly StartDate { get; set; }
	public DateOnly EndDate { get; set; }
	public bool IsFavorite { get; set; }
}

public class DashboardFavoriteGoalItem
{
	public int GoalId { get; set; }
	public string GoalName { get; set; } = string.Empty;
	public decimal ProgressPercent { get; set; }
	public int CompletedResponses { get; set; }
	public int TargetResponseCount { get; set; }
}

public class GoalEditModel
{
	public int? Id { get; set; }

	[Required]
	[StringLength(200)]
	public string Name { get; set; } = string.Empty;

	[StringLength(2000)]
	public string? Description { get; set; }

	public int? AreaId { get; set; }

	public int? SurveyDefinitionId { get; set; }

	[Range(1, int.MaxValue)]
	public int TargetResponseCount { get; set; } = 100;

	public DateOnly StartDate { get; set; } = DateOnly.FromDateTime(DateTime.UtcNow.Date);
	public DateOnly EndDate { get; set; } = DateOnly.FromDateTime(DateTime.UtcNow.Date.AddMonths(1));
	public IReadOnlyList<SelectOption> AreaOptions { get; set; } = Array.Empty<SelectOption>();
	public IReadOnlyList<SelectOption> SurveyOptions { get; set; } = Array.Empty<SelectOption>();
}

public class ReportingOverviewModel
{
	public int TotalResponses { get; set; }
	public int MappedResponses { get; set; }
	public int UnmappedResponses { get; set; }
	public int GoalCount { get; set; }
	public int ZipMappingCount { get; set; }
	public IReadOnlyList<AreaResponseReportItem> AreaResponses { get; set; } = Array.Empty<AreaResponseReportItem>();
	public IReadOnlyList<GoalProgressReportItem> GoalProgress { get; set; } = Array.Empty<GoalProgressReportItem>();
	public IReadOnlyList<UnmappedPostalCodeReportItem> UnmappedPostalCodes { get; set; } = Array.Empty<UnmappedPostalCodeReportItem>();
}

public class AreaResponseReportItem
{
	public int AreaId { get; set; }
	public string AreaName { get; set; } = string.Empty;
	public int ResponseCount { get; set; }
	public int GoalCount { get; set; }
}

public class GoalProgressReportItem
{
	public int GoalId { get; set; }
	public string GoalName { get; set; } = string.Empty;
	public string? AreaName { get; set; }
	public string? SurveyName { get; set; }
	public int TargetResponseCount { get; set; }
	public int CompletedResponses { get; set; }
	public int RemainingResponses { get; set; }
	public decimal ProgressPercent { get; set; }
	public DateOnly StartDate { get; set; }
	public DateOnly EndDate { get; set; }
}

public class UnmappedPostalCodeReportItem
{
	public string PostalCode { get; set; } = string.Empty;
	public int ResponseCount { get; set; }
}
