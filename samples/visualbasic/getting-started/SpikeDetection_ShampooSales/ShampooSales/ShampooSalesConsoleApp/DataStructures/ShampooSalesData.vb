﻿Imports Microsoft.ML.Data

Namespace ShampooSales.DataStructures
	Public Class ShampooSalesData
		<LoadColumn(0)>
		Public Month As String

		<LoadColumn(1)>
		Public numSales As Single
	End Class
End Namespace
