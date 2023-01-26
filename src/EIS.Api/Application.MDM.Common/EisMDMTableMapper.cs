using System.Runtime.Serialization.Json;
using System.Text.Json;

namespace EIS.Api.Application.MDM.Common;

public class EisMDMTableMapper
{
    public static object MapTableToSerializedObject(string tableName, string payloadContent)
    {
        object payloadContractCommand = null;

        if (tableName.Equals(MasterDatabaseContentTypes.Category))
        {
            payloadContractCommand = JsonSerializer.Deserialize<CategoryContract>(payloadContent);
        }
        else if (tableName.Equals(MasterDatabaseContentTypes.SubCategory))
        {
            payloadContractCommand = JsonSerializer.Deserialize<SubCategoryContract>(payloadContent);
        }

        return payloadContractCommand;
    }
}