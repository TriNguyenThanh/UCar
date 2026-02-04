using Microsoft.EntityFrameworkCore;
using UCar.Models;
using UCar.Models.Enums;

namespace UCar.Data.Seeders;

/// <summary>
/// Seeder for Shifts, Shift Assignments, and Operational Tasks (Module 8.0)
/// </summary>
public static class OperationsSeeder
{
    public static async Task SeedShiftsAndTasksAsync(
        UCarDbContext context,
        List<StaffProfile> staffList,
        List<Branch> branches,
        List<Vehicle> vehicles)
    {
        Console.WriteLine("Seeding Shifts, ShiftAssignments, and OperationalTasks (Module 8.0)...");

        // Create Shifts
        var shifts = new List<Shift>
        {
            new()
            {
                ShiftId = Guid.NewGuid(),
                ShiftName = "Ca sáng",
                StartTime = new TimeOnly(7, 0),
                EndTime = new TimeOnly(12, 0),
                BranchId = branches[0].BranchId,
                Description = "Ca sáng từ 7h-12h",
                IsActive = true
            },
            new()
            {
                ShiftId = Guid.NewGuid(),
                ShiftName = "Ca chiều",
                StartTime = new TimeOnly(12, 0),
                EndTime = new TimeOnly(17, 0),
                BranchId = branches[0].BranchId,
                Description = "Ca chiều từ 12h-17h",
                IsActive = true
            },
            new()
            {
                ShiftId = Guid.NewGuid(),
                ShiftName = "Ca tối",
                StartTime = new TimeOnly(17, 0),
                EndTime = new TimeOnly(22, 0),
                BranchId = branches[0].BranchId,
                Description = "Ca tối từ 17h-22h",
                IsActive = true
            },
            new()
            {
                ShiftId = Guid.NewGuid(),
                ShiftName = "Ca toàn thời gian",
                StartTime = new TimeOnly(8, 0),
                EndTime = new TimeOnly(17, 0),
                BranchId = branches[1].BranchId,
                Description = "Ca làm việc cả ngày 8h-17h",
                IsActive = true
            }
        };

        await context.Shifts.AddRangeAsync(shifts);
        await context.SaveChangesAsync();

        // Create Shift Assignments for next 7 days
        var shiftAssignments = new List<ShiftAssignment>();
        var random = new Random(456);
        var today = DateOnly.FromDateTime(DateTime.Today);

        for (int day = 0; day < 7; day++)
        {
            var workDate = today.AddDays(day);

            // Assign 2-3 staff to each shift
            foreach (var shift in shifts.Take(3))
            {
                var assignedStaff = staffList.OrderBy(_ => random.Next()).Take(2).ToList();
                foreach (var staff in assignedStaff)
                {
                    shiftAssignments.Add(new ShiftAssignment
                    {
                        AssignmentId = Guid.NewGuid(),
                        ShiftId = shift.ShiftId,
                        StaffId = staff.StaffId,
                        WorkDate = workDate,
                        CreatedBy = staffList[0].UserId,
                        CreatedAt = DateTime.UtcNow,
                        Notes = null
                    });
                }
            }
        }

        await context.ShiftAssignments.AddRangeAsync(shiftAssignments);
        await context.SaveChangesAsync();

        // Create Operational Tasks
        var operationalTasks = new List<OperationalTask>();
        var availableVehicles = vehicles.Where(v => v.CurrentStatus == VehicleStatus.Available).Take(10).ToList();
        var taskTypes = new[] { TaskType.Delivery, TaskType.Return, TaskType.Maintenance, TaskType.Rescue, TaskType.Inspection };
        var taskStatuses = new[] { OpTaskStatus.New, OpTaskStatus.Assigned, OpTaskStatus.InProgress, OpTaskStatus.Completed };

        for (int i = 0; i < 15; i++)
        {
            var taskType = taskTypes[i % taskTypes.Length];
            var status = taskStatuses[i % taskStatuses.Length];
            var vehicle = i < availableVehicles.Count ? availableVehicles[i] : null;
            var staff = status != OpTaskStatus.New ? staffList[i % staffList.Count] : null;
            var scheduledAt = DateTime.UtcNow.AddHours(-24 + (i * 4));

            var task = new OperationalTask
            {
                TaskId = Guid.NewGuid(),
                TaskType = taskType,
                Title = taskType switch
                {
                    TaskType.Delivery => $"Giao xe {vehicle?.PlateNo ?? "N/A"} cho khách",
                    TaskType.Return => $"Nhận xe {vehicle?.PlateNo ?? "N/A"} từ khách",
                    TaskType.Maintenance => $"Bảo dưỡng định kỳ xe {vehicle?.PlateNo ?? "N/A"}",
                    TaskType.Rescue => $"Cứu hộ xe {vehicle?.PlateNo ?? "Khẩn cấp"}",
                    TaskType.Inspection => $"Kiểm tra xe {vehicle?.PlateNo ?? "N/A"}",
                    _ => "Nhiệm vụ vận hành"
                },
                VehicleId = vehicle?.VehicleId,
                AssignedToStaffId = staff?.StaffId,
                BranchId = branches[i % branches.Count].BranchId,
                ScheduledAt = scheduledAt,
                EstimatedDurationMinutes = taskType == TaskType.Maintenance ? 120 : 30,
                Location = taskType == TaskType.Rescue ? $"Đường {i + 1}, Quận {(i % 5) + 1}" : null,
                Status = status,
                CompletedAt = status == OpTaskStatus.Completed ? scheduledAt.AddMinutes(25) : null,
                Notes = status == OpTaskStatus.Completed ? "Hoàn thành đúng hẹn" : null,
                CreatedBy = staffList[0].UserId,
                CreatedAt = scheduledAt.AddHours(-2)
            };

            operationalTasks.Add(task);
        }

        await context.OperationalTasks.AddRangeAsync(operationalTasks);
        await context.SaveChangesAsync();

        Console.WriteLine($"  ✓ Created {shifts.Count} shifts, {shiftAssignments.Count} shift assignments");
        Console.WriteLine($"  ✓ Created {operationalTasks.Count} operational tasks");
    }
}
