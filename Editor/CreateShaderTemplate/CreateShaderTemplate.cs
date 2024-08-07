using UnityEditor;

public class CreateShaderTemplate : Editor
{
    public const string ScriptTemplatePath = "Packages/WoBok's Plugins/Editor/CreateShaderTemplate/ShaderTemplates/";

    [MenuItem("Assets/Create/Shader/Shader1", priority = 1)]
    static void CreateIComponentDataScript()
    {
        ProjectWindowUtil.CreateScriptAssetFromTemplateFile($"{ScriptTemplatePath}URP_PBR_Shader.txt", "NewIComponentDataScript.cs");
    }

    [MenuItem("\"Assets/Create/Shader/Shader2", priority = 2)]
    static void CreateISystemScript()
    {
        ProjectWindowUtil.CreateScriptAssetFromTemplateFile($"{ScriptTemplatePath}ISystemTemplate.txt", "NewISystemScript.cs");
    }

    [MenuItem("\"Assets/Create/Shader/Shader3", priority = 3)]
    static void CreateIJobEntityScript()
    {
        ProjectWindowUtil.CreateScriptAssetFromTemplateFile($"{ScriptTemplatePath}IJobEntityTemplate.txt", "NewIJobEntityScript.cs");
    }

    [MenuItem("\"Assets/Create/Shader/Shader4", priority = 4)]
    static void CreateBakerScript()
    {
        ProjectWindowUtil.CreateScriptAssetFromTemplateFile($"{ScriptTemplatePath}BakerTemplate.txt", "NewBakerScript.cs");
    }
}