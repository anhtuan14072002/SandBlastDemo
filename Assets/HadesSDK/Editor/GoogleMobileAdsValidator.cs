using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace HadesSDK
{
    public class GoogleMobileAdsValidator : Validator
    {
        public override bool ValidateError(out string error)
        {
            error = "";
            string folder = Path.Combine(Application.dataPath, "GoogleMobileAds", "Resources");
            string filePath = Path.Combine(folder, "GoogleMobileAdsSettings.asset");
            
            if (!Directory.Exists(folder) || !File.Exists(filePath))
            {
                error = "Google mobile ads app id invalid";
                return true;
            }
            
            try
            {
                var assetPath = "Assets/GoogleMobileAds/Resources/GoogleMobileAdsSettings.asset";
                var dictionary = ParseScriptableObject(assetPath);
                
                foreach (var keyValuePair in dictionary)
                {
                    if (keyValuePair.Key == "adMobAndroidAppId")
                    {
                        if (string.IsNullOrEmpty(keyValuePair.Value))
                        {
                            error = "Google mobile ads app id invalid";
                            return true;
                        }
                        break;
                    }
                }

            }
            catch (Exception e)
            {
                Debug.LogError(e);
                return false;
            }

            return false;
        }
        
        public static Dictionary<string, string> ParseScriptableObject(string assetPath)
        {
            var dictionary = new Dictionary<string, string>();

            // Load the ScriptableObject from the asset file
            var obj = AssetDatabase.LoadAssetAtPath<ScriptableObject>(assetPath);
            if (obj == null)
            {
                Debug.LogError("Failed to load asset at path: " + assetPath);
                return dictionary;
            }

            // Create a SerializedObject from the ScriptableObject
            var serializedObject = new SerializedObject(obj);
            var property = serializedObject.GetIterator();

            // Iterate through all properties
            if (property.NextVisible(true))
            {
                do
                {
                    if (property.propertyType != SerializedPropertyType.Generic && property.propertyType == SerializedPropertyType.String)
                    {
                        dictionary[property.name] = property.stringValue;
                    }
                }
                while (property.NextVisible(false));
            }

            return dictionary;
        }
    }
}