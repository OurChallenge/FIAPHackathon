using Hackathon.Application.DTOs.Campaigns;
using Hackathon.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hackathon.Api.Controllers;

[ApiController]
[Route("api/campaigns")]
public class CampaignsController : ControllerBase
{
    private readonly CampaignService _campaignService;

    public CampaignsController(CampaignService campaignService)
    {
        _campaignService = campaignService;
    }

    [Authorize(Roles = "GestorONG")]
    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateCampaignRequest request)
    {
        try
        {
            var campaignId = await _campaignService.CreateAsync(request);

            return Created(string.Empty, new
            {
                Id = campaignId,
                Message = "Campaign created successfully."
            });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new
            {
                Message = ex.Message
            });
        }
    }

    [Authorize(Roles = "GestorONG")]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateCampaignRequest request)
    {
        try
        {
            await _campaignService.UpdateAsync(id, request);

            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new
            {
                Message = ex.Message
            });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new
            {
                Message = ex.Message
            });
        }
    }

    [AllowAnonymous]
    [HttpGet("active")]
    public async Task<ActionResult<IEnumerable<CampaignTransparencyResponse>>>
        GetActive()
    {
        var campaigns =
            await _campaignService.GetActiveCampaignsAsync();

        return Ok(campaigns);
    }
}