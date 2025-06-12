using System;
using System.Collections.Generic;
using Zakup.Abstractions.Data;
using Zakup.Common.Enums;

namespace Zakup.Common.DTO.Zakup;

public class ReturnToMainMenuCallbackData : ICallbackData
{
    public Guid ZakupId { get; set; }

    public string ToCallback()
    {
        return $"{ZakupId}";
    }

    public void Parse(List<string> data)
    {
        ZakupId = Guid.Parse(data[0]);
    }
} 