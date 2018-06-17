Feature: VanillaOptionDisplayer
	Check the conversion and display of vanilla option

@mytag
Scenario: Display a vanilla option table data
	Given The Vanilla Option deal with
	| Underlying | Currency | Strike | OptionType | Quantity |
	| AL         | USD      | 2327   | Call       | 1        |
	| XAU        | USD      | 1200   | Call       | 2        |
	| BL         | EUR      | 48     | Call       | 3        |
	Then the correct result will be displayed
