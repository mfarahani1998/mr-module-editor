namespace MRModuleEditor.Core.StepTypes
{
    public static class StepCatalogBuilder
    {
        public static StepCatalog CreateDefaultCatalog()
        {
            StepCatalog catalog = new StepCatalog();
            BuiltInStepDefinitions.Register(catalog);
            return catalog;
        }
    }
}
