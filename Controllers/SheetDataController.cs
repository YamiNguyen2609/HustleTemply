using HustleTemply.Models;
using HustleTemply.Services;
using Microsoft.AspNetCore.Mvc;

namespace HustleTemply.Controllers;

[ApiController]
[Route("api/sheet-data")]
public class SheetDataController : ControllerBase
{
    private readonly IGoogleSheetService _sheetService;
    private readonly ISheetExportService _exportService;

    public SheetDataController(
        IGoogleSheetService sheetService,
        ISheetExportService exportService)
    {
        _sheetService = sheetService;
        _exportService = exportService;
    }

    /// <summary>
    /// Returns all parsed sheet sections using the configured default spreadsheet.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(SheetDataResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<SheetDataResponse>> GetAll(CancellationToken cancellationToken)
    {
        var data = await _sheetService.GetAllDataAsync(cancellationToken).ConfigureAwait(false);
        return Ok(data);
    }

    /// <summary>
    /// Returns parsed sheet data for a specific spreadsheet (verify before copy).
    /// </summary>
    [HttpGet("{sheetId}")]
    [ProducesResponseType(typeof(SheetDataResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SheetDataResponse>> GetBySheetId(
        string sheetId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(sheetId))
            return BadRequest("sheetId is required.");

        try
        {
            var data = await _sheetService.GetAllDataAsync(sheetId, cancellationToken).ConfigureAwait(false);
            return Ok(data);
        }
        catch (Google.GoogleApiException ex) when (ex.HttpStatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return NotFound($"Spreadsheet '{sheetId}' was not found or is not accessible.");
        }
    }

    /// <summary>
    /// Copies a spreadsheet, shares the copy with the submitter, and returns export data.
    /// </summary>
    [HttpPost("copy-and-email")]
    [ProducesResponseType(typeof(CopyAndEmailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CopyAndEmailResponse>> CopyAndEmail(
        [FromBody] CopyAndEmailRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        try
        {
            var result = await _exportService.CopyAndEmailAsync(request, cancellationToken).ConfigureAwait(false);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
    }

    /// <summary>
    /// Returns configuration items from the sheet.
    /// </summary>
    [HttpGet("configurations")]
    [ProducesResponseType(typeof(List<ConfigurationItem>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<ConfigurationItem>>> GetConfigurations(CancellationToken cancellationToken)
    {
        var data = await _sheetService.GetAllDataAsync(cancellationToken).ConfigureAwait(false);
        return Ok(data.Configurations);
    }

    /// <summary>
    /// Returns product items with joined image URLs.
    /// </summary>
    [HttpGet("products")]
    [ProducesResponseType(typeof(List<ProductItem>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<ProductItem>>> GetProducts(CancellationToken cancellationToken)
    {
        var data = await _sheetService.GetAllDataAsync(cancellationToken).ConfigureAwait(false);
        return Ok(data.Products);
    }

    /// <summary>
    /// Returns category items from the sheet.
    /// </summary>
    [HttpGet("categories")]
    [ProducesResponseType(typeof(List<CategoryItem>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<CategoryItem>>> GetCategories(CancellationToken cancellationToken)
    {
        var data = await _sheetService.GetAllDataAsync(cancellationToken).ConfigureAwait(false);
        return Ok(data.Categories);
    }

    /// <summary>
    /// Returns social link items from the sheet.
    /// </summary>
    [HttpGet("socials")]
    [ProducesResponseType(typeof(List<SocialItem>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<SocialItem>>> GetSocials(CancellationToken cancellationToken)
    {
        var data = await _sheetService.GetAllDataAsync(cancellationToken).ConfigureAwait(false);
        return Ok(data.Socials);
    }
}
