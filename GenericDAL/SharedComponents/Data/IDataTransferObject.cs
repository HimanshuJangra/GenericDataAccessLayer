namespace SharedComponents.Data
{
    /// <summary>
    /// DTO's Definition
    /// </summary>
    public interface IDataTransferObject
    {
        /// <summary>
        /// Help Identify if Primary Key exists and has a value
        /// </summary>
        bool HasPrimaryKey { get; }

        /// <summary>
        /// current state of the data
        /// </summary>
        System.Data.DataRowState RowState { get; set; }
    }
}