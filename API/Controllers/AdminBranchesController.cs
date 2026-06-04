using AutoWashPro.BLL.DTOs;
using AutoWashPro.BLL.Services;
using CloudinaryDotNet.Actions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace AutoWashPro.API.Controllers
{
    [ApiController]
    [Route("api/v1/branches")]
    [Authorize]
    public class AdminBranchesController : ControllerBase
    {
        private readonly IBranchService _branchService;

        public AdminBranchesController(IBranchService branchService)
        {
            _branchService = branchService;
        }

        [HttpGet]
        [Authorize(Roles = "Customer, Admin")]
        public async Task<IActionResult> GetAllBranches()
        {
            var branches = await _branchService.GetAllBranchesAsync();
            return Ok(branches);
        }

        [HttpGet("{id}")]
        [Authorize(Roles = "Customer, Admin")]
        public async Task<IActionResult> GetBranch(int id)
        {
            var branch = await _branchService.GetBranchByIdAsync(id);
            return Ok(branch);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateBranch([FromBody] CreateBranchDTO dto)
        {
            var branch = await _branchService.CreateBranchAsync(dto);
            return CreatedAtAction(nameof(GetBranch), new { id = branch.BranchId }, branch);
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateBranch(int id, [FromBody] UpdateBranchDTO dto)
        {
            var branch = await _branchService.UpdateBranchAsync(id, dto);
            return Ok(branch);
        }
    }
}
