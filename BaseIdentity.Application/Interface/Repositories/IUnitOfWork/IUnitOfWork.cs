namespace BaseIdentity.Application.Interface.Repositories.IUnitOfWork
{
    public interface IUnitOfWork : IDisposable
    {
        // Thêm các repository khác nếu cần

        /// <summary>
        /// Commit các thay đổi trên toàn bộ các repository.
        /// </summary>
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
