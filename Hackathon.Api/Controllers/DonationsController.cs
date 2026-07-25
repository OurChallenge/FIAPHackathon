using System.Security.Claims;
using Hackathon.Application.DTOs.Donations;
using Hackathon.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hackathon.Api.Controllers;

[ApiController]
[Route("api/donations")]
[Authorize(Roles = "Doador")]
public class DonationsController : ControllerBase
{
    private readonly DonationService _donationService;

    public DonationsController(DonationService donationService)
    {
        _donationService = donationService;
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateDonationRequest request)
    {
        try
        {
            var userIdClaim =
                User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? User.FindFirstValue("sub");

            if (!Guid.TryParse(userIdClaim, out var donorId))
            {
                return Unauthorized(new
                {
                    Message = "Invalid user identifier."
                });
            }

            var donationId = await _donationService.CreateAsync(
                donorId,
                request);

            return Accepted(new
            {
                Id = donationId,
                Message = "Donation received and queued for processing."
            });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new
            {
                Message = ex.Message
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new
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
}