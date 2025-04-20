    /// <summary>
    /// Utilizza la reflection per registrare automaticamente tutte le costanti string definite in AppPermissions come policy
    /// </summary>
    private static void AddAllPermissionsAsPolicies(AuthorizationOptions options)
    {
        // Ottiene tutte le classi annidate in AppPermissions
        var nestedTypes = typeof(AppPermissions).GetNestedTypes(BindingFlags.Public | BindingFlags.Static);
        
        foreach (var nestedType in nestedTypes)
        {
            // Ottiene tutti i campi costanti di tipo string nella classe
            var fields = nestedType.GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)
                .Where(f => f.IsLiteral && !f.IsInitOnly && f.FieldType == typeof(string));
            
            foreach (var field in fields)
            {
                // Ottiene il valore del campo (il nome della permission)
                string permission = (string)field.GetValue(null);
                
                // Se la policy non è già stata aggiunta, la aggiungiamo
                if (!options.PolicyMap.ContainsKey(permission))
                {
                    options.AddPolicy(permission, policy =>
                        policy.Requirements.Add(new PermissionRequirement(permission)));
                }
            }
        }
    }