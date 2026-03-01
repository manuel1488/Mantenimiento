using App.Core.Common;
using App.Core.DTOs.Label;

namespace App.Core.Interfaces.Shop;

public interface IBulkLabelService
{
    /// <summary>
    /// Creates a bulk label job record and returns its data.
    /// </summary>
    Task<Result<BulkLabelJobDto>> CreateAsync(CreateBulkLabelJobDto dto, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a bulk label job by ID.
    /// </summary>
    Task<Result<BulkLabelJobDto>> GetByIdAsync(long id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the most recent bulk label jobs.
    /// </summary>
    Task<Result<List<BulkLabelJobDto>>> GetRecentAsync(int count = 50, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates a label PDF for a saved job.
    /// </summary>
    Task<Result<byte[]>> GetLabelPdfAsync(long id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates a label PDF preview without saving to database.
    /// </summary>
    Task<Result<byte[]>> PreviewLabelPdfAsync(CreateBulkLabelJobDto dto, CancellationToken cancellationToken = default);
}
