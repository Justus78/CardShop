﻿using CardShop.Models;

namespace api.Interfaces
{
    public interface ITokenService
    {
        string CreateToken(ApplicationUser user);
    }
}
