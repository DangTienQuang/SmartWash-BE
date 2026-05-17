using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoWashPro.BLL.DTOs;
using AutoWashPro.DAL.Data;
using AutoWashPro.DAL.Entities;
using Microsoft.EntityFrameworkCore;
using PayOS;
using PayOS.Models.V2.PaymentRequests;

using Microsoft.Extensions.Logging;

namespace AutoWashPro.BLL.Services
{
    public class WalletService : IWalletService
    {
        private readonly AutoWashDbContext _context;
        private readonly PayOSClient _payOSClient;
        private readonly ILogger<WalletService> _logger;

        public WalletService(AutoWashDbContext context, PayOSClient payOSClient, ILogger<WalletService> logger)
        {
            _context = context;
            _payOSClient = payOSClient;
            _logger = logger;
        }

        public async Task<WalletResponseDTO> GetWalletInfoAsync(int userId)
        {
            var wallet = await _context.Wallets.FirstOrDefaultAsync(w => w.UserId == userId);
            if (wallet == null)
            {
                wallet = new Wallet { UserId = userId, Balance = 0, Status = "Active" };
                _context.Wallets.Add(wallet);
                await _context.SaveChangesAsync();
            }

            var totalPoints = await _context.PointLedgers
                .Where(p => p.UserId == userId)
                .SumAsync(p => p.PointsAdded - p.PointsDeducted);

            return new WalletResponseDTO
            {
                Balance = wallet.Balance,
                TotalPoints = totalPoints
            };
        }

        public async Task<TopUpResponseDTO> CreateTopUpLinkAsync(int userId, TopUpRequestDTO request)
        {
            var orderCode = DateTimeOffset.Now.ToUnixTimeSeconds();
            var paymentRequest = new CreatePaymentLinkRequest
            {
                OrderCode = orderCode,
                Amount = (int)request.Amount,
                Description = $"Topup wallet {userId}",
                CancelUrl = request.CancelUrl,
                ReturnUrl = request.ReturnUrl
            };

            var createPaymentResult = await _payOSClient.PaymentRequests.CreateAsync(paymentRequest);

            return new TopUpResponseDTO
            {
                PaymentUrl = createPaymentResult.CheckoutUrl,
                OrderCode = orderCode.ToString()
            };
        }

        public async Task ProcessPaymentWebhookAsync(WebhookTopUpDTO webhookData)
        {
            _logger.LogInformation("Nhận được Webhook giả lập: {OrderCode}, Mã lỗi: {Code}", 
                webhookData.Data?.OrderCode, webhookData.Code);

            var data = webhookData.Data; 

            if (data != null && webhookData.Code == "00")
            {
                var orderCodeStr = data.OrderCode.ToString();
                var alreadyProcessed = await _context.Transactions
                    .AnyAsync(t => t.Description.Contains($"(Mã: {orderCodeStr})") && t.TransactionType == "Topup");
                
                if (alreadyProcessed) 
                {
                    _logger.LogWarning("Giao dịch {OrderCode} đã được xử lý trước đó.", data.OrderCode);
                    return;
                }

                int userId = 0;
                var desc = data.Description ?? "";
                
                // Sử dụng Regex để tìm con số cuối cùng trong chuỗi description (ví dụ: "Topup wallet 2" -> 2)
                var match = System.Text.RegularExpressions.Regex.Match(desc, @"\d+$");
                if (match.Success)
                {
                    int.TryParse(match.Value, out userId);
                }

                if (userId > 0)
                {
                    var wallet = await _context.Wallets.FirstOrDefaultAsync(w => w.UserId == userId);
                    if (wallet == null)
                    {
                        wallet = new Wallet { UserId = userId, Balance = 0, Status = "Active" };
                        _context.Wallets.Add(wallet);
                    }

                    wallet.Balance += data.Amount;
                    
                    var transaction = new Transaction
                    {
                        WalletId = wallet.WalletId,
                        Amount = data.Amount,
                        TransactionType = "Topup",
                        Description = $"Nạp tiền thành công (Mã: {data.OrderCode})",
                        CreatedAt = DateTime.UtcNow
                    };
                    
                    _context.Transactions.Add(transaction);
                    await _context.SaveChangesAsync();
                    
                    _logger.LogInformation("Cập nhật số dư thành công cho User {UserId}. Số tiền: {Amount}", userId, data.Amount);
                }
                else
                {
                    _logger.LogError("Không thể trích xuất UserId từ description: {Description}", desc);
                }
            }
            else
            {
                _logger.LogWarning("Webhook không hợp lệ hoặc thanh toán thất bại. Code: {Code}", webhookData.Code);
            }
        }

        public async Task<List<TransactionResponseDTO>> GetTransactionsAsync(int userId)
        {
            var wallet = await _context.Wallets.FirstOrDefaultAsync(w => w.UserId == userId);
            if (wallet == null) return new List<TransactionResponseDTO>();

            return await _context.Transactions
                .Where(t => t.WalletId == wallet.WalletId)
                .OrderByDescending(t => t.CreatedAt)
                .Select(t => new TransactionResponseDTO
                {
                    TransactionId = t.TransactionId,
                    Amount = t.Amount,
                    TransactionType = t.TransactionType,
                    Description = t.Description,
                    CreatedAt = t.CreatedAt
                }).ToListAsync();
        }

        public async Task<List<PointHistoryResponseDTO>> GetPointsHistoryAsync(int userId)
        {
            return await _context.PointLedgers
                .Where(p => p.UserId == userId)
                .OrderByDescending(p => p.TransactionDate)
                .Select(p => new PointHistoryResponseDTO
                {
                    LedgerId = p.LedgerId,
                    PointsAdded = p.PointsAdded,
                    PointsDeducted = p.PointsDeducted,
                    Reason = p.Reason,
                    TransactionDate = p.TransactionDate
                }).ToListAsync();
        }

        public async Task DeductPointsFIFOAsync(int userId, int pointsToDeduct, string reason)
        {
            var totalPoints = await _context.PointLedgers
                .Where(p => p.UserId == userId)
                .SumAsync(p => p.PointsAdded - p.PointsDeducted);

            if (totalPoints < pointsToDeduct)
                throw new Exception("Không đủ điểm để thực hiện giao dịch này.");

            var ledger = new PointLedger
            {
                UserId = userId,
                PointsDeducted = pointsToDeduct,
                Reason = reason,
                TransactionDate = DateTime.UtcNow
            };

            _context.PointLedgers.Add(ledger);
            await _context.SaveChangesAsync();
        }
    }
}
