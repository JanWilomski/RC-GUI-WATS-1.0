   M S B A M L   `   `   `  ����I  ERiskCheckerGUI, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null;"clr-namespace:RiskCheckerGUI.ViewsRiskCheckerGUI.Views  5      U/clr-namespace:RiskCheckerGUI.Helpers.Converters!RiskCheckerGUI.Helpers.Converters  5      '    !RiskCheckerGUI.Views.MessagesView   5      W SPresentationCore, Version=8.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35\ XPresentationFramework, Version=8.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35e aSystem.Windows.Controls.Ribbon, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089R NWindowsBase, Version=8.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35F 9http://schemas.microsoft.com/winfx/2006/xaml/presentation     5      R NSystem.Xaml, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     5      Jmc;http://schemas.openxmlformats.org/markup-compatibility/2006     5      @d2http://schemas.microsoft.com/expression/blend/2008     5      4local"clr-namespace:RiskCheckerGUI.Views     5      F
converters/clr-namespace:RiskCheckerGUI.Helpers.Converters     5        � 	Resources  5      >   8RiskCheckerGUI.Helpers.Converters.BytesToStringConverter   BytesToStringConverterA   ;RiskCheckerGUI.Helpers.Converters.BoolToVisibilityConverter  BoolToVisibilityConverterB   <RiskCheckerGUI.Helpers.Converters.PercentageToColorConverter  PercentageToColorConverter  
LimitStyle �� 
TargetType �� Property �� Value��%W  &	        &	      &	 &     &	 9       5   
   6,     5   
   6/     5   
   60   �� 5   
   " ��6#   .�5      ��  � ��
 �   �6+   �� 5       � ��
 ���� 6+   �� 5       �@��TextAlignment$ Center=�6.   �� 5       �@��Padding$	 5,2q�6(   �� 5       � ��$ 2q�6'   5      5      .��5      �  � RowDefinitions 5   
   �� 5      $
��Auto �6   �� 5      $��* �6   �� 5      $��* �6   �� 5      $��* �6   .+�5"   
   � $��0��6    � ColumnDefinitions 5#      �� 5$      $	��150 �6#   �� 5%      $��* �6#   .+�5(      � $��0��6    5)      �� 5*      $
��Auto �6$   �� 5+      $
��Auto �6$   �� 5,      $
��Auto �6$   �� 5-      $
��Auto �6$   �� 5.      $
��Auto �6$   �� 5/      $
��Auto �6$   .+�52      �� $��0��6   $��Capital��6)   $
��Bold"�68   $
��10,5q�6J   �� 54      $��1��6   $��Open Capital:��6)   $
��10,2q�6>   �� 55      $��2��6   $��Accrued Capital:��6)   $
��10,2q�6A   �� 56      $��3��6   $��Total Capital:��6)   $
��10,2q�6?   �� 58      $��4��6   $��Limits��6)   $
��Bold"�67   $
��10,5q�6I   �� 59      $��5��6   $��	Messages:��6)   $
��10,2q�6:   �� 5:      $��6��6   $��Capital:��6)   $
��10,2q�69   5;      � 5=      $��1��6    5>      �� 5?      $
��Auto �6$   �� 5@      $
��Auto �6$   �� 5A      $
��Auto �6$   �� 5B      $
��Auto �6$   �� 5C      $
��Auto �6$   �� 5D      $
��Auto �6$   �� 5E      $
��Auto �6$   .+�5H      �� $��0��6   $�� ��6)   $
��10,5q�62   �� 5K      $��1��6   $
��10,2q�6�   ��6)   �� *CapitalUsage.OpenCapital+ �� StringFormat$ N2�� �� FallbackValue$
 0.00�� �� TargetNullValue$
 0.00��6�   �� 5L      $��2��6   $
��10,2q�6�   ��6)   �� *CapitalUsage.AccruedCapital+$ N2��$
 0.00��$
 0.00��6�   �� 5M      $��3��6   $
��10,2q�6�   ��6)   �� *CapitalUsage.TotalCapital+$ N2��$
 0.00��$
 0.00��6�   �� 5O      $��4��6   $�� ��6)   $
��10,5q�62   �� 5Q      #��[ 6   ��5R      �� *" CapitalUsage.MessageUsagePercent+	 �� 	Converter#	 [ $ Transparent����5S      `� 5T      $ {0:F1}% of {1}��6'   .1�5U      �� 
 �� Path$&
  CapitalUsage.MessageUsagePercent�6&   $	 0.0��6N   �� 5V      $
 CapitalUsage.MessageLimit�6&   $ 0��6G   5W      5Y      �� 5\      #��[ 6   ��5]      �� *" CapitalUsage.CapitalUsagePercent+#	 [ $ Transparent����5^      `� 5_      $ {0:F1}% of {1}��6'   .1�5`      �� $&
  CapitalUsage.CapitalUsagePercent�6&   $	 0.0��6N   �� 5a      $
 CapitalUsage.CapitalLimit�6&   $ 0��6G   5b      5d      5e      5f      � 5i   
   $��1��6    5j      �� 5k      $
��Auto �6    �� 5l      $��* �6    .+�5o      �� $��0��6    �� Orientation$ 
Horizontal=�6&   ���#F0F0F0.+�5p      �� $��Instruments��6   $
��Bold"�6/   $��5q�6A   �� 5q      $	��150��6   $��5q�6&    � ToolTip$ Filter instruments��5r      ��5q   1   �� *InstrumentFilter+ �� UpdateSourceTrigger$ PropertyChanged=�5r      5s      &   System.Windows.Controls.DataGrid  5u      $��1��6      AutoGenerateColumns$ False��5v         
IsReadOnly$
 True��5w         CanUserResizeColumns$
 True��5x         CanUserSortColumns$
 True��5y         HorizontalGridLinesBrush �#E0E0E0   VerticalGridLinesBrush �#E0E0E0   AlternatingRowBackground �#F9F9F9��5u   $   �� *InstrumentPositions+   Columns 5}      0  *System.Windows.Controls.DataGridTextColumn  5~      ,  &System.Windows.Controls.DataGridColumn   Header$
 ISIN��6)      Width5  /System.Windows.Controls.DataGridLengthConverter$	 120 6P   1  +System.Windows.Controls.DataGridBoundColumn   Binding 67   �� *ISIN+6P     5      $ Ticker Name��6)   $	 120 6]    6>   �� *
TickerName+6]     5�      $	 Net��6)   $ 80 6N    66   �� *Net+6N     5�      $ 	Open Long��6)   $ 80 6Y    6<   �� *
OpenLong+6Y     5�      $ 
Open Short��6)   $ 80 6[    6=   �� *	OpenShort+6[   5�      5�      � 5�   
   $��2��6    5�      �� 5�      $
��Auto �6    �� 5�      $��* �6    .+�5�      �� $��0��6   $ 
Horizontal=�6&   ���#F0F0F0.+�5�      �� $��CCG Messages��6   $
��Bold"�60   $��5q�6B   �� 5�      $	��150��6   $��5q�6&   $ Filter messages��5�      ��5�   1   �� *MessageFilter+$ PropertyChanged=�5�      �� 5�      $��Auto scroll��6    d� 	IsChecked$
 TrueY�61   $��5q�6B   $��Center=�6M   5�        5�      $��1��6   $ False��5�      $
 True��5�      $
 True��5�      $
 True��5�       �#E0E0E0 �#E0E0E0 �#F9F9F9��5�   $   �� *CcgMessages+ 5�        5�      $ Nr��6)   $ 40 6L    65   �� *Nr+6L     5�      $ Header��6)   $ 60 6T    69   �� *Header+6T     5�      $
 Name��6)   $	 120 6P    67   �� *Name+6P     5�      $ 	MsgSeqNum��6)   $ 80 6Z    6<   �� *	MsgSeqNum+6Z     5�      $ Date received��6)   $	 150 6�    6@   �� *DateReceived+$ yyyy-MM-dd HH:mm:ss.fff��6�     5�      $ Transact time��6)   $	 120 6a    6@   �� *TransactTime+6a     5�      $ Price��6)   $ 80 6c    68   �� *Price+$ N2��6c     5�      $
 Side��6)   $ 60 6P    67   �� *Side+6P     5�      $ Symbol��6)   $ 60 6T    69   �� *Symbol+6T     5�      $ ClOrdID��6)   $	 120 6V    6:   �� *	ClOrdID+6V   5�      5�      � 5�   
   $��3��6    5�      �� 5�      $
��Auto �6    �� 5�      $��* �6    .+�5�      �� $��0��6   $ 
Horizontal=�6&   ���#F0F0F0.+�5�      �� $��
Order Book��6   $
��Bold"�6.   $��5q�6@   �� 5�      $	��150��6   $��5q�6&   $ Filter orders��5�      ��5�   1   �� *OrderBookFilter+$ PropertyChanged=�5�      �� 5�      $��Auto scroll��6   $
 TrueY�61   $��5q�6B   $��Center=�6M   5�        5�      $��1��6   $ False��5�      $
 True��5�      $
 True��5�      $
 True��5�       �#E0E0E0 �#E0E0E0 �#F9F9F9��5�   $   �� *	OrderBook+ 5�        5�      $ OrderID��6)   $	 150 6V    6:   �� *	OrderID+6V     5�      $ Transact time��6)   $	 120 6a    6@   �� *TransactTime+6a     5�      $
 Side��6)   $ 60 6P    67   �� *Side+6P     5�      $ Ticker��6)   $ 80 6T    69   �� *Ticker+6T     5�      $ Price��6)   $ 80 6c    68   �� *Price+$ N2��6c     5�      $ OrderQty��6)   $ 70 6X    6;   �� *
OrderQty+6X     5�      $ CumQty��6)   $ 70 6T    69   �� *CumQty+6T     5�      $ 	LeavesQty��6)   $ 70 6Z    6<   �� *	LeavesQty+6Z     5�      $ Market��6)   $ 60 6V    69   �� *
MarketID+6V     5�      $ Account��6)   $ 60 6V    6:   �� *	Account+6V     5�      $ Last modified��6)   $	 120 6a    6@   �� *LastModified+6a     5�      $ OrigID��6)   $	 150 6Y    69   �� *OrigOrderID+6Y     5�      $
 Text��6)   $ * 6P    67   �� *Text+6P      RowStyle 5�      �� 5�      )	  #System.Windows.Controls.DataGridRow" 	 6    �� Triggers 5�      �� 5�       �� Binding 6*   �� *
IsActive+ �� Value	 False6G   .a�5�   "   ��  � ��
 �����6?   5�      5�      5�      5�      5�      5�      