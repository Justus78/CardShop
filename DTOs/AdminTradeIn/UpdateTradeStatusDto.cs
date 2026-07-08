using api.Models;
using System.ComponentModel.DataAnnotations;

namespace api.DTOs.AdminTradeIn
{
    public class UpdateTradeStatusDto
    {
        [Required]
        public int Id { get; set; }
        [Required]
        public TradeInStatus Status { get; set; }   
    }
}
