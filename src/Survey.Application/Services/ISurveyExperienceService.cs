using Survey.Application.Models;

namespace Survey.Application.Services;

public interface ISurveyExperienceService
{
	Task<SiteAppearanceModel> GetSiteAppearanceAsync(CancellationToken cancellationToken = default);
	Task<SurveySessionModel?> GetPublicSessionAsync(string token, CancellationToken cancellationToken = default);
	Task<SurveySessionModel?> GetStaffSessionAsync(int assignmentId, CancellationToken cancellationToken = default);
	Task<SubmitSurveyResult> SubmitAsync(SurveySubmissionModel model, string? submittedByUserId, CancellationToken cancellationToken = default);
}
