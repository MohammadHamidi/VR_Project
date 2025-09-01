using UnityEngine;

public class BridgeValidator : IBridgeValidator
{
    public ValidationResult Validate(BridgeData bridgeData)
    {
        var result = new ValidationResult { IsValid = true };
        var errors = new System.Collections.Generic.List<string>();
        var warnings = new System.Collections.Generic.List<string>();

        if (bridgeData.Planks == null || bridgeData.Planks.Length == 0)
        {
            errors.Add("No planks found in bridge");
            result.IsValid = false;
        }

        foreach (var plank in bridgeData.Planks ?? new IBridgeComponent[0])
        {
            if (plank?.GameObject == null)
            {
                errors.Add("Invalid plank found");
                result.IsValid = false;
            }
            else if (plank.GameObject.GetComponent<Rigidbody>() == null)
            {
                warnings.Add($"Plank {plank.GameObject.name} missing Rigidbody");
            }
        }

        if (bridgeData.Platforms != null)
        {
            foreach (var platform in bridgeData.Platforms)
            {
                if (platform?.GameObject == null)
                {
                    errors.Add("Invalid platform found");
                    result.IsValid = false;
                }
            }
        }

        result.Errors = errors.ToArray();
        result.Warnings = warnings.ToArray();
        return result;
    }
}