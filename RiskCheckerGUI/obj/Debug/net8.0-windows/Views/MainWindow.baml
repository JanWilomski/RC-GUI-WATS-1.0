   M S B A M L   `   `   `  ����I  ERiskCheckerGUI, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null;"clr-namespace:RiskCheckerGUI.ViewsRiskCheckerGUI.Views  5   	   %    RiskCheckerGUI.Views.MainWindow   5   	   W SPresentationCore, Version=8.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35\ XPresentationFramework, Version=8.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35e aSystem.Windows.Controls.Ribbon, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089R NWindowsBase, Version=8.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35F 9http://schemas.microsoft.com/winfx/2006/xaml/presentation     5   	   R NSystem.Xaml, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     5   	   @d2http://schemas.microsoft.com/expression/blend/2008     5   	   Jmc;http://schemas.openxmlformats.org/markup-compatibility/2006     5   	   4views"clr-namespace:RiskCheckerGUI.Views     5   	     � Title$  Risk Checker GUI��5   	   $	��600��6"   $	��800��6/   .��5	      �  � RowDefinitions 5
   
   �� 5      $
��Auto �6   �� 5      $��* �6   .+�5   
   �� $��0��6   $��0,0,0,1q�6   ���#CCCCCC���#F5F5F5._�5      � $
��10,5q�6    � ColumnDefinitions 5      �� 5      $
��Auto �6'   �� 5      $
��Auto �6'   �� 5      $
��Auto �6'   �� 5      $
��Auto �6'   �� 5      $
��Auto �6'   �� 5      $
��Auto �6'   �� 5      $
��Auto �6'   �� 5      $��* �6'   �� 5      $
��Auto �6'   .+�5      �� $��0��6   $��Host:��6,   $��Center=�69   $��0,0,5,0q�6T   �� 5      $��1��6   $	��120��6@   $��0,0,10,0q�6L   ��6*   �� *Host+6L   �� 5!      $��2��6   $��	TCP Port:��6,   $��Center=�6=   $��0,0,5,0q�6X   �� 5"      $��3��6   $��60��6C   $��0,0,10,0q�6N   ��6*   �� *	TcpPort+6N   �� 5$      $��4��6   $��
Multicast:��6,   $��Center=�6>   $��0,0,5,0q�6Y   �� 5%      $��5��6   $	��100��6J   $��0,0,10,0q�6V   ��6*   �� *MulticastGroup+6V   �� 5'      $��6��6   $��	UDP Port:��6,   $��Center=�6=   $��0,0,5,0q�6X   �� 5(      $��7��6   $��60��6C   $��0,0,10,0q�6N   ��6*   �� *	UdpPort+6N   �� 5*      $��8��6    �� Orientation$ 
Horizontal=�6-   .+�5,      �� $��Connect��6@   $��80��6R   $��0,0,5,0q�6]   ��6   �� *ConnectCommand+{�5-      �� *IsConnected+ �� 	Converter Z� �� 5.      $��
Disconnect��6C   $��80��6X   ��6   �� *DisconnectCommand+{�5/      �� *IsConnected+50      �� 51      $��Test Connection��6   $	��100��63   $	��5,0q�6?   ��6L   �� *TestConnectionCommand+�� 55      $��9��6   $��2��6,   $��Center=�56      $��10,0,0,0q�67   ��55   @   �� *StatusMessage+56   7   57      58      �� 5:   
   $��Refresh Data��6   $	��100��6(   $	��5,0q�64   ��6A   �� *RefreshDataCommand+�� 5=   
   $��1��6   .@�5>      �� $��Messages��6   .��5?      '   !RiskCheckerGUI.Views.MessagesView  5@      �� 5A      $��Settings��6   .��5B      '   !RiskCheckerGUI.Views.SettingsView  5C      �� 5D      $��Filters��6   .��5E      &    RiskCheckerGUI.Views.FiltersView  5F      �� 5G      $��Instruments��6   .��5H      *   $RiskCheckerGUI.Views.InstrumentsView  5I      5J      5K      5L      