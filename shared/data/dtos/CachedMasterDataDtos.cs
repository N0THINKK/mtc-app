namespace mtc_app.shared.data.dtos
{
    /// <summary>
    /// Cached machine data for offline dropdowns.
    /// </summary>
    public class CachedMachineDto
    {
        public int MachineId { get; set; }
        public string Code { get; set; }
        public string MachineType { get; set; }
        public string MachineArea { get; set; }
        public string MachineNumber { get; set; }
        public int StatusId { get; set; }
    }

    /// <summary>
    /// Cached shift data for offline dropdowns.
    /// </summary>
    public class CachedShiftDto
    {
        public int ShiftId { get; set; }
        public string ShiftName { get; set; }
    }

    /// <summary>
    /// Cached problem type data for offline dropdowns.
    /// </summary>
    public class CachedProblemTypeDto
    {
        public int TypeId { get; set; }
        public string TypeName { get; set; }
    }

    /// <summary>
    /// Cached failure data for offline dropdowns.
    /// </summary>
    public class CachedFailureDto
    {
        public int FailureId { get; set; }
        public string FailureName { get; set; }
    }

    /// <summary>
    /// Cached cause data for offline dropdowns.
    /// </summary>
    public class CachedCauseDto
    {
        public int CauseId { get; set; }
        public string CauseName { get; set; }
    }

    /// <summary>
    /// Cached action data for offline dropdowns.
    /// </summary>
    public class CachedActionDto
    {
        public int ActionId { get; set; }
        public string ActionName { get; set; }
    }
    /// <summary>
    /// Cached part data for offline dropdowns.
    /// </summary>
    public class CachedPartDto
    {
        public int PartId { get; set; }
        public string PartCode { get; set; }
        public string PartName { get; set; }
    }
}
