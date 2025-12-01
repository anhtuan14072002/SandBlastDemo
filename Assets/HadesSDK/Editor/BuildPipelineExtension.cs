using System.Collections;
using System.Collections.Generic;
using System.Text;
using HadesSDK;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class BuildPipelineExtension
{
    static BuildPipelineExtension()
    {
        BuildPlayerWindow.RegisterBuildPlayerHandler(OnBuild);
    }
    
    private static void OnBuild( BuildPlayerOptions options )
    {
        BuildProjectWithConfirmation(options);
    }

    static void BuildProjectWithConfirmation(BuildPlayerOptions options)
    {
        StringBuilder stringBuilder = new StringBuilder();

        List<Validator> validators = new List<Validator>()
        {
            new GoogleMobileAdsValidator()
        };

        bool hasWarningOrError = false;
        
        foreach (var validator in validators)
        {
            if (validator.ValidateError(out var error))
            {
                hasWarningOrError = true;
                stringBuilder.Append(error);
                stringBuilder.AppendLine();
            }
        }

        if (hasWarningOrError)
        {
            bool proceedWithBuild = EditorUtility.DisplayDialog(
                "Build Project",
                stringBuilder.ToString(),
                "Yes",
                "No"
            );

            if (proceedWithBuild)
            {
                
            }
        }
        else
        {
            BuildPlayerWindow.DefaultBuildMethods.BuildPlayer(options);
        }
    }
}
