using System.Reflection;
using System.Text.Json;
using ModelContextProtocol.Server;
using TradingMcp.Tools;

namespace XTests.McpServer;

public class When_Tool_Schemas_Are_Generated
{
    public static TheoryData<string> ToolMethods => new(
        typeof(TradingTools).GetMethods().Where(m => m.GetCustomAttribute<McpServerToolAttribute>() is not null).Select(m => m.Name));

    // A nullable parameter such as DateOnly? becomes "type": ["string", "null"]. That's legal JSON Schema,
    // but clients that only accept a single type (such as Gemini's function declarations) may reject the tool.
    [Theory]
    [MemberData(nameof(ToolMethods))]
    public void it_should_give_every_parameter_a_single_type(string methodName)
    {
        var tool = McpServerTool.Create(typeof(TradingTools).GetMethod(methodName)!, _ => null!);

        foreach (var parameter in tool.ProtocolTool.InputSchema.GetProperty("properties").EnumerateObject())
        {
            Assert.Equal(JsonValueKind.String, parameter.Value.GetProperty("type").ValueKind);
        }
    }
}
