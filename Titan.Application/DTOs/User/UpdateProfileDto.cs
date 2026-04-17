using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Titan.Application.DTOs.User;


public record UpdateProfileDto(string FirstName,
    string LastName,
    string Phone, string PreferredLanguage);
