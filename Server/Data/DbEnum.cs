namespace Server.Data
{
    /// <summary>
    /// Builds CHECK constraint SQL from an enum, so the database only accepts the values the code knows.
    /// Enums are stored by name (HasConversion&lt;string&gt;()), so the member names are the allowed values.
    /// </summary>
    public static class DbEnum<CurEnum> where CurEnum : struct, Enum
    {
        /// <summary>SQL for a CHECK constraint, for example: [status] IN (N'Open', N'Paid').</summary>
        public static string CheckSql(string column) =>
            $"[{column}] IN ({string.Join(", ", Enum.GetNames<CurEnum>().Select(name => $"N'{name}'"))})";
    }
}
