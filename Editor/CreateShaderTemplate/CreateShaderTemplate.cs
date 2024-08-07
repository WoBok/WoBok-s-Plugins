using UnityEditor;

public class CreateShaderTemplate : Editor
{
    [MenuItem("Assets/Create/Shader/URP Shader", false, -959)]
    static void CreateURPShaderFile()
    {
        CreateShaderFile("URPShader", "New URP Shader.shader");
    }
    [MenuItem("Assets/Create/Shader/URP PBR Shader", false, -958)]
    static void CreateURPPBRShaderFile()
    {
        CreateShaderFile("URP_PBR_Shader", "New URP PBR Shader.shader");
    }
    static void CreateShaderFile(string filePath, string defaultNewFileName)
    {
        ProjectWindowUtil.StartNameEditingIfProjectWindowExists(
            0,
            CreateInstance<DoCreateShaderScritpAsset>(),
            defaultNewFileName,
            null,
            filePath
            );
    }
}