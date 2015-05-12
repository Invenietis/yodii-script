#region LGPL License
/*----------------------------------------------------------------------------
* This file (SharedAssemblyInfo.cs) is part of Yodii-Script. 
*  
* Yodii-Script is free software: you can redistribute it and/or modify 
* it under the terms of the GNU Lesser General Public License as published 
* by the Free Software Foundation, either version 3 of the License, or 
* (at your option) any later version. 
*  
* Yodii-Script is distributed in the hope that it will be useful, 
* but WITHOUT ANY WARRANTY; without even the implied warranty of
* MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the 
* GNU Lesser General Public License for more details. 
* You should have received a copy of the GNU Lesser General Public License 
* along with Yodii-Script. If not, see <http://www.gnu.org/licenses/>. 
*  
* Copyright © 2007-2015, 
*     Invenietis <http://www.invenietis.com>, IN'TECH INFO <http://www.intechinfo.fr>
* All rights reserved. 
*-----------------------------------------------------------------------------*/
#endregion

using System;
using System.Reflection;

[assembly: AssemblyProduct("Yodii.Script")]
[assembly: AssemblyCompany("Invenietis")]
[assembly: AssemblyCopyright("Copyright (c) Invenietis, IN'TECH INFO 2013-2015")]
[assembly: AssemblyTrademark("")]

[assembly: AssemblyVersion("0.6.0")]


#if DEBUG
    [assembly: AssemblyConfiguration("Debug")]
#else
    [assembly: AssemblyConfiguration("Release")]
#endif

// Added by CKReleaser.
[assembly: AssemblyInformationalVersion( "%ck-standard%" )]
