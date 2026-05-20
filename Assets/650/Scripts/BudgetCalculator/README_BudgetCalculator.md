# Budget Calculator

Runtime flow:

1. `BudgetCalculatorBootstrap` creates `BudgetCalculatorManager` after the scene loads.
2. The manager loads `Resources/Budget/BudgetCalculatorCatalog`.
3. The catalog initializes one default `0 EUR` option per category.
4. `BudgetCalculatorUI` is added automatically to `Panel_Calculadora` or `Panel Calculadora` when found.
5. `BudgetInteractionMaterialListener` listens to `InteractObject.OnAnyInteract` and maps the transferred material name to a catalog option.

For precise manual setup, add `MaterialCostOption` to the same object that has `MaterialTransferHandler` and assign one `BudgetMaterialOptionSO`.
That component subscribes to `MaterialTransferHandler.onTransferComplete` and sends the selected option to the manager.

Edit prices and visible names in:

`Assets/650/Resources/Budget/Options`

Edit the list of available options in:

`Assets/650/Resources/Budget/BudgetCalculatorCatalog.asset`
