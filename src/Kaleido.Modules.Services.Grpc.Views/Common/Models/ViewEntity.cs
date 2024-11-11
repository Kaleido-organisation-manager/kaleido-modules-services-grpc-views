using Kaleido.Common.Services.Grpc.Models;

namespace Kaleido.Modules.Services.Grpc.Views.Common.Models;

public class ViewEntity : BaseEntity
{
    public string Name { get; set; } = string.Empty;

    public override bool Equals(object? obj)
    {
        if (obj == null || GetType() != obj.GetType())
        {
            return false;
        }

        return Name == ((ViewEntity)obj).Name;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Id, Name);
    }
}