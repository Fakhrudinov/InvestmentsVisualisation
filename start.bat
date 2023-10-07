cd "c:\Users\alex\source\repos\InvestmentsVisualisation\InvestmentVisualisation\InvestmentVisualisation\bin\Release\net6.0\publish\"
start "InvestmentVisualisation" "InvestmentVisualisation.exe" --urls "https://localhost:5101"
timeout /T 2 /NOBREAK
start https://localhost:5101