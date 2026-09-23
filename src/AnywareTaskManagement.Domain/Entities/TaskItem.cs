using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using DomainTaskStatus = AnywareTaskManagement.Domain.Enums.TaskStatus;
using AnywareTaskManagement.Domain.Enums;

namespace AnywareTaskManagement.Domain.Entities;

public class TaskItem
{
    public Guid Id { get; private set; }

    public string Title { get; private set; } = string.Empty;

    public string Description { get; private set; } = string.Empty;

    public DomainTaskStatus Status { get; private set; }

    public TaskPriority Priority { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public Guid UserId { get; private set; }

    private TaskItem()
    {
    }

    public TaskItem(
        string title,
        string description,
        TaskPriority priority,
        Guid userId)
    {
        Id = Guid.NewGuid();
        Title = title;
        Description = description;
        Priority = priority;
        UserId = userId;
        Status = DomainTaskStatus.Pending;
        CreatedAt = DateTime.UtcNow;
    }

    public void UpdateStatus(DomainTaskStatus status)
    {
        Status = status;
    }
}