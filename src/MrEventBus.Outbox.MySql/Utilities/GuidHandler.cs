using Dapper;
using System.Data;
namespace MrEventBus.Box.MySql.Utilities
{
    public class GuidHandler : SqlMapper.TypeHandler<Guid>
    {
        public override void SetValue(IDbDataParameter parameter, Guid value)
        {
            parameter.Value = value.ToString();
        }

        public override Guid Parse(object value)
        {
            return Guid.Parse(value.ToString());
        }
    }
}
