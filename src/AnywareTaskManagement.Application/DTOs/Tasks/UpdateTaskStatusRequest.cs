using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using AnywareTaskManagement.Domain.Enums;
using DomainTaskStatus = AnywareTaskManagement.Domain.Enums.TaskStatus;
namespace AnywareTaskManagement.Application.DTOs.Tasks;

public class UpdateTaskStatusRequest
{
    public DomainTaskStatus Status { get; set; }
}
