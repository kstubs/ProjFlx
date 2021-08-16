<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform" xmlns:wbt="myWebTemplater.1.0" exclude-result-prefixes="wbt">
	<xsl:variable name="wbt:inc-name">WBT_TABLES.xsl</xsl:variable>
	
	<xsl:template match="wbt:default-table" mode="identity-translate">		
		<xsl:comment>Identity Translate: wbt:default-table - <xsl:value-of select="$wbt:inc-name"/></xsl:comment>
		<xsl:choose>
			<xsl:when test="@force-grid-view">
				<xsl:apply-templates select="key('wbt:key_Results', current()/@query)" mode="wbt:default-table">
					<xsl:with-param name="force-grid-view" select="@force-grid-view"/>
				</xsl:apply-templates>
			</xsl:when>
			<xsl:otherwise>
				<xsl:apply-templates select="key('wbt:key_Results', current()/@query)" mode="wbt:default-table"/>				
			</xsl:otherwise>
		</xsl:choose>
		
	</xsl:template>

	<xsl:template match="wbt:query" mode="identity-translate">
		<xsl:comment>Identity Translate: wbt:query - <xsl:value-of select="$wbt:inc-name"/></xsl:comment>
		<xsl:variable name="project" select="@project"/>
		<xsl:variable name="query">
			<xsl:choose>
				<xsl:when test="string(@name)">
					<xsl:value-of select="@name"/>
				</xsl:when>
				<xsl:otherwise>
					<xsl:value-of select="@query"/>
				</xsl:otherwise>
			</xsl:choose>
		</xsl:variable>
		<xsl:variable name="projsql" select="$projSql/descendant-or-self::projectSql/*[local-name() = $project]/query[@name = $query]"/>
		<xsl:variable name="results" select="key('wbt:key_QueryResults', $query)"/>
		<xsl:variable name="numrows" select="count($results/result/row)"/>
		<xsl:variable name="force-grid-view" select="$results/schema/query/@force-grid-view = 'true' or @force-grid-view = 'true'"/>

		<xsl:if test="not(@hide-empty-results = 'yes' and $numrows=0)">
			<div class="container">
				<ol class="breadcrumb">
					<li>ProjSql</li>
					<li>
						<xsl:value-of select="$project"/>
					</li>
					<li class="active">
						<xsl:value-of select="$query"/>
					</li>
				</ol>
			</div>
			
			<xsl:if test="$projsql">
				<div>
					<a href="#" class="float-end" wbt-toggle="search_options">
						<i class="fa fa-bars"/>
					</a>
				</div>
				<div class="row clear" id="search_options">
					<xsl:if test="$numrows > 0 or @hide-options = 'true'">
						<xsl:attribute name="style">display:none</xsl:attribute>
					</xsl:if>
					<div class="col-md-4">
						<xsl:choose>
							<xsl:when test="@action = 'NonQuery' or @action = 'Scalar'">
								<xsl:apply-templates select="." mode="wbt:execute">
									<xsl:with-param name="project" select="$project"/>
									<xsl:with-param name="query" select="$query"/>
									<xsl:with-param name="action" select="@action"/>
								</xsl:apply-templates>
							</xsl:when>
							<xsl:otherwise>
								<xsl:apply-templates select="$projsql/parameters" mode="wbt:default-table-search">
									<xsl:with-param name="context" select="."/>
									<xsl:with-param name="project" select="$project"/>
									<xsl:with-param name="query" select="$query"/>
								</xsl:apply-templates>
							</xsl:otherwise>
						</xsl:choose>
					</div>
					<div class="col-md-8">
						<div class="panel">
							<xsl:if test="string($projsql/comment())">
								<pre>
								<xsl:value-of select="$projsql/comment()"/>					
							</pre>
							</xsl:if>
						</div>
						<div class="panel">
							<pre>
							<xsl:apply-templates select="$projsql" mode="simple-wbt-defaultquery"/>
						</pre>
							<hr/>
							<pre>
							<xsl:apply-templates select="$projsql" mode="simple-wbt-query"/>
						</pre>
						</div>
					</div>
				</div>
			</xsl:if>
			<xsl:variable name="actions" select="$projSql/*[local-name() = $project]/query[@name = $query]/actions"/>

			<div class="clear tab-control">
				<ul class="nav nav-tabs" role="tablist">
					<li role="presentation" class="active">
						<a href="#home" aria-controls="home" role="tab" data-toggle="tab">
							<xsl:value-of select="$query"/>
						</a>
					</li>
					<xsl:choose>
						<xsl:when test="$numrows = 1">
							<xsl:for-each select="$actions/action[@type != 'link']">
								<li role="presentation">
									<a href="#edits_{@type}" aria-controls="home" role="tab" data-toggle="tab">
										<xsl:choose>
											<xsl:when test="text()">
												<xsl:value-of select="text()"/>
											</xsl:when>
											<xsl:otherwise>
												<xsl:value-of select="@type"/>
											</xsl:otherwise>
										</xsl:choose>
									</a>
								</li>
							</xsl:for-each>
						</xsl:when>
					</xsl:choose>
					<xsl:call-template name="wbt:query.custom-actions.tabs">
						<xsl:with-param name="project" select="$project"/>
						<xsl:with-param name="query" select="@query"/>
						<xsl:with-param name="action" select="@type"/>
					</xsl:call-template>
				</ul>
				<div class="tab-content">
					<div role="tabpanel" class="tab-pane active" id="home">
						<xsl:apply-templates select="$results" mode="wbt:default-table">
							<xsl:with-param name="caption" select="''"/>
							<xsl:with-param name="force-grid-view" select="$force-grid-view"/>
						</xsl:apply-templates>
						<xsl:if test="not($results)">
							<table class="table table-bordered">
								<xsl:call-template name="wbt:default-table.no-results"/>
							</table>
						</xsl:if>
					</div>
					<xsl:variable name="current" select="current()"/>
					<xsl:for-each select="$projSql/*[local-name() = $project]/query[@name = $query]/actions/action">
						<div role="tabpanel" class="tab-pane" id="edits_{@type}">
							<xsl:apply-templates select="$current" mode="wbt:edits">
								<xsl:with-param name="project" select="$project"/>
								<xsl:with-param name="query" select="@query"/>
								<xsl:with-param name="action" select="@type"/>
							</xsl:apply-templates>
						</div>
					</xsl:for-each>
					<xsl:apply-templates select="." mode="wbt:query.custom-actions.tab-panes">
						<xsl:with-param name="project" select="$project"/>
						<xsl:with-param name="query" select="@query"/>
						<xsl:with-param name="action" select="@type"/>
					</xsl:apply-templates>
				</div>
			</div>
		</xsl:if>
	</xsl:template>
	
	<xsl:template match="*" mode="simple-wbt-defaultquery">
		<xsl:text disable-output-escaping="no">		<![CDATA[<wbt:default-table query="]]></xsl:text>
		<xsl:value-of select="@name"/>
		<xsl:text disable-output-escaping="no"><![CDATA["]]></xsl:text>
		<xsl:text disable-output-escaping="no"><![CDATA[ force-grid-view="true" xmlns:wbt="myWebTemplater.1.0"/>]]></xsl:text>
	</xsl:template>
	<xsl:template match="*" mode="simple-wbt-query">
		<xsl:text disable-output-escaping="no">		<![CDATA[<wbt:query project="]]></xsl:text>
		<xsl:value-of select="local-name(parent::*)"/>		
		<xsl:text disable-output-escaping="no">" <![CDATA[query="]]></xsl:text>
		<xsl:value-of select="@name"/>
		<xsl:text disable-output-escaping="no"><![CDATA["]]></xsl:text>
		<xsl:text disable-output-escaping="no"><![CDATA[ xmlns:wbt="myWebTemplater.1.0"/>]]></xsl:text>
	</xsl:template>
	
	<xsl:template name="wbt:query.custom-actions.tabs">
		<xsl:param name="project" />
		<xsl:param name="query" />
		<xsl:param name="action" />		
	</xsl:template>
	
	<xsl:template match="wbt:query" mode="wbt:query.custom-actions.tab-panes">
		<xsl:param name="project" />
		<xsl:param name="query" />
		<xsl:param name="action" />		
	</xsl:template>
	
	<xsl:template match="parameters" mode="wbt:default-table-search">
		<xsl:param name="context" select="/.."/>
		<xsl:param name="project" />
		<xsl:param name="query" />
		<xsl:comment>Template Match 'default-table-search': parameters - <xsl:value-of select="$wbt:inc-name"/></xsl:comment>
		<form method="GET" action="">
			<xsl:attribute name="name">
				<xsl:value-of select="ancestor::query/@name"/>
			</xsl:attribute>
			
			<xsl:apply-templates select="parameter" mode="wbt:default-table-search">
				<xsl:with-param name="context" select="$context"/>
			</xsl:apply-templates>
			<div class="form-group col-md-offset-9 col-md-3">
				<input type="submit" class="btn btn-primary form-control float-end" name="submit" value="submit"/>
			</div>
			<input type="hidden" name="wbt_project" value="{$project}"/>
			<input type="hidden" name="wbt_query" value="{$query}"/>
		</form>		
	</xsl:template>
	
	<xsl:template match="parameter" mode="wbt:default-table-search">
		<xsl:param name="context" select="/.."/>
		<xsl:comment>Template Match 'default-table-search': parameter - <xsl:value-of select="$wbt:inc-name"/></xsl:comment>
		<xsl:variable name="parm-name" select="@name"/>
		<xsl:variable name="context-parm" select="$context/parameters/parameter[@name=$parm-name]"/>
		<div class="form-group">
			<label for="{@name}">
				<xsl:choose>
					<xsl:when test="string($context-parm/@display)">
						<xsl:value-of select="$context-parm/@display"/>
					</xsl:when>
					<xsl:when test="string(@display)">
						<xsl:value-of select="@display"/>
					</xsl:when>
					<xsl:otherwise>
						<xsl:value-of select="@name"/>
					</xsl:otherwise>
				</xsl:choose>
			</label>
			<input type="text" class="form-control" name="{@name}">
				<xsl:variable name="var" select="$context/ancestor::flx/proj/browser/*/element[@name=current()/@name]"/>
				<xsl:attribute name="value">
					<xsl:choose>
						<xsl:when test="string($var)">
							<xsl:value-of select="$var"/>
						</xsl:when>
						<xsl:when test="string($context-parm/text())">
							<xsl:choose>
								<!-- test for reminents of inline regex lookup tactic -->
								<xsl:when test="starts-with($context-parm/text(),'{query') or starts-with($context-parm/text(),'{form') or starts-with($context-parm/text(),'{cook') or starts-with($context-parm/text(),'{sess')">
									<!-- revert to projSqlXml parameter value -->
									<xsl:value-of select="text()"/>
								</xsl:when>
								<xsl:otherwise>
									<xsl:value-of select="$context-parm/text()"/>
								</xsl:otherwise>
							</xsl:choose>
						</xsl:when>
						<xsl:otherwise>
							<xsl:value-of select="text()"/>
						</xsl:otherwise>
					</xsl:choose>
				</xsl:attribute>
			</input>
		</div>
	</xsl:template>
	
	<!-- Table Matches -->
    <xsl:template match="results | result" mode="wbt:default-table">
        <xsl:param name="force-grid-view" select="false()"/>
        <xsl:param name="caption" select="@name"/>
    	<xsl:comment>Template Match 'wbt:default-table': results - <xsl:value-of select="$wbt:inc-name"/></xsl:comment>
    	
    	<xsl:apply-templates select="." mode="wbt:default-table.table">
			<xsl:with-param name="force-grid-view" select="$force-grid-view"/>
			<xsl:with-param name="caption" select="$caption"/>
		</xsl:apply-templates>
    </xsl:template>
	
	<xsl:template match="results | result" mode="wbt:default-table.table">
		<xsl:param name="force-grid-view" select="false()"/>
		<xsl:param name="caption" select="@name"/>
		<xsl:comment>Template Match 'default-table.table': results - <xsl:value-of select="$wbt:inc-name"/></xsl:comment>
		<xsl:if test="$caption">
	        <h3>
	            <xsl:value-of select="$caption"/>
	        </h3>			
		</xsl:if>
        <table class="table table-bordered table-hover table-striped" wbt-query="{@name}">
            <xsl:choose>
                <xsl:when test="count(descendant-or-self::result/row) &gt; 1 or $force-grid-view">
                    <tr>                    	
                    	<xsl:if test="schema/query/actions/action[@type='link']">
                    		<th>-</th>
                    	</xsl:if>
                    	<xsl:if test="schema/query/actions/action[@type[.='update' or .='insert' or .='delete']]">
                    		<th>-</th>
                    	</xsl:if>
                    	<xsl:apply-templates select="schema/query/fields/field" mode="wbt:default-table-multirow-header"/>
                    	<xsl:if test="not(schema/query/fields/field)">
                    		<xsl:apply-templates select="descendant-or-self::result/row[1]" mode="wbt:default-table-multirow-header"/>
                    	</xsl:if>
                    </tr>
                	<xsl:apply-templates select="descendant-or-self::result/row" mode="wbt:default-table-multirow"/>                    
                </xsl:when>
            	<xsl:otherwise>
            		<xsl:apply-templates select="descendant-or-self::result/row" mode="wbt:default-table"/>
                </xsl:otherwise>
            </xsl:choose>

        	<xsl:if test="count(result/row | row) = 0">
            	<xsl:variable name="offset-count">
            		<xsl:choose>
            			<xsl:when test="schema/query/actions/action">1</xsl:when>
            			<xsl:otherwise>0</xsl:otherwise>
            		</xsl:choose>
            	</xsl:variable>
            	<xsl:call-template name="wbt:default-table.no-results">
            		<xsl:with-param name="col-count" select="number($offset-count) + count(schema/query/fields/field[not(@ForView='false')])"/>
            	</xsl:call-template>
            </xsl:if>
        </table>
    	<xsl:choose>
    		<xsl:when test="schema/query/paging/pages/@totalrecords &gt; count(result/row)">
		        <xsl:call-template name="wbt:paging"/>    		    			
    		</xsl:when>
    	</xsl:choose>
    </xsl:template>
	
	<xsl:template name="wbt:default-table.no-results">
		<xsl:param name="col-count" select="1"/>
		<xsl:param name="help-text"/>
		<xsl:comment>Template Name 'wbt:default-table.no-results' - <xsl:value-of select="$wbt:inc-name"/></xsl:comment>
		<xsl:comment>Project: <xsl:value-of select="@project"/>, Query: <xsl:value-of select="@name"/></xsl:comment>
		<tr>
			<td colspan="{$col-count}">
				<div class="jumbotron">
					<h1 class="text-center">No Results</h1>
					<xsl:if test="string($help-text)">
						<p>
							<em><xsl:value-of select="$help-text" disable-output-escaping="yes"/></em>
						</p>
					</xsl:if>
				</div>
			</td>
		</tr>
	</xsl:template>


    <xsl:template match="row" mode="wbt:default-table">
    	<xsl:apply-templates select="ancestor::results[1]/schema/query/fields/field" mode="wbt:default-table">
            <xsl:with-param name="current" select="."/>
        </xsl:apply-templates>
    </xsl:template>

    <xsl:template match="field" mode="wbt:default-table">
        <xsl:param name="current" select="/"/>
        <xsl:param name="field_name" select="@name"/>
        <tr>
            <th>
                <xsl:value-of select="$field_name"/>
            </th>
            <td>
                <xsl:value-of select="$current/@*[name()=$field_name]" disable-output-escaping="yes"/>
            </td>
        </tr>
    </xsl:template>

    <xsl:template match="field[@GridView='false' or @ForView='false']" mode="wbt:default-table-multirow" priority="2"/>
    <xsl:template match="field[@GridView='false' or @ForView='false']" mode="wbt:default-table-multirow-header" priority="2"/>

	<!-- match on row when no schema field names provided, use raw fields name from first row --> 
    <xsl:template match="row | field" mode="wbt:default-table-multirow-header">
    	<xsl:param name="row-field.index" select="1"/>
    	<xsl:variable name="header">
    		<xsl:choose>
    			<xsl:when test="self::row">
    				<xsl:value-of select="local-name(self::row/@*[$row-field.index])"/>
    			</xsl:when>
    			<xsl:otherwise>
    				<xsl:choose>
    					<xsl:when test="@GridViewDisplay">
    						<xsl:value-of select="@GridViewDisplay"/>
    					</xsl:when>
    					<xsl:when test="@display">
    						<xsl:value-of select="@display"/>
    					</xsl:when>
    					<xsl:otherwise>
    						<xsl:value-of select="@name"/>
    					</xsl:otherwise>
    				</xsl:choose>
    			</xsl:otherwise>
    		</xsl:choose>
    	</xsl:variable>
        <th>
        	<xsl:attribute name="wbt-field">
        		<xsl:value-of select="@name"/>
        	</xsl:attribute>
        	<xsl:value-of select="$header"/>
        </th>
    	<xsl:if test="self::row and $row-field.index &lt; count(@*)">
    		<xsl:apply-templates select="self::row" mode="wbt:default-table-multirow-header">
    			<xsl:with-param name="row-field.index" select="$row-field.index + 1"/>
    		</xsl:apply-templates>
    	</xsl:if>
    </xsl:template>

	<xsl:template match="row" mode="wbt:default-table-multirow">
		<xsl:variable name="action" select="ancestor::results/schema/query/actions/action"/>
		<tr>
			<xsl:apply-templates select="." mode="wbt:default-table-multirow.attributes"/>
			<xsl:if test="$action[@type='link']">
				<xsl:apply-templates select="." mode="wbt:default-table-multirow.link"/>
			</xsl:if>
			<xsl:if test="$action[@type='update' or @type='delete' or @type='insert']">
				<xsl:apply-templates select="." mode="wbt:default-table-multirow.editable"/>
			</xsl:if>
			<xsl:choose>
				<xsl:when test="ancestor::subquery">
					<xsl:apply-templates select="@*" mode="wbt:default-table-multirow"/>
				</xsl:when>
				<xsl:otherwise>                    
					<xsl:apply-templates select="ancestor::results[1]/schema/query/fields/field" mode="wbt:default-table-multirow">
						<xsl:with-param name="current" select="."/>
					</xsl:apply-templates>
					<!-- write out every field on the row when no fields defined in schema -->
					<xsl:if test="not(ancestor::results[1]/schema/query/fields/field)">
						<xsl:apply-templates select="@*" mode="wbt:default-table-multirow"/>
					</xsl:if>
				</xsl:otherwise>
			</xsl:choose>
		</tr>
	</xsl:template>

	<xsl:template match="row" mode="wbt:default-table-multirow.attributes">
		<xsl:param name="current" select="."/>
	</xsl:template>
	
	<xsl:template match="row" mode="wbt:default-table-multirow.editable">
		<th><a href="#"><i class="fa fa-pencil"/></a></th>		
	</xsl:template>
	
	<xsl:template match="row" mode="wbt:default-table-multirow.link">
		<xsl:variable name="result.project" select="ancestor::results/schema/query/@project"/>
		<xsl:variable name="result.query" select="ancestor::results/schema/query/@name"/>
		<xsl:variable name="action" select="ancestor::results/schema/query/actions/action[@type = 'link']"/>
		<xsl:variable name="action.link" select="concat('wbt_project=',$result.project, '&amp;wbt_query=', $action/@query)"/>		
		<xsl:variable name="primary-id" select="@*[local-name() = $action/@primaryKey]"/>				
		<th>
			<a>
				<xsl:attribute name="href">
					<xsl:value-of select="concat('?', $action.link, '&amp;', $action/@queryPrimaryKey, '=', $primary-id)"/>
					<xsl:value-of select="concat('&amp;', 'LookupIndex=', $action/@LookupIndex)"/>
					<xsl:if test="$action/@LookupIndex">
					</xsl:if>
				</xsl:attribute>
				<i class="fa fa-link"/>
			</a>
		</th>		
	</xsl:template>
	
    <xsl:template match="field | @*" mode="wbt:default-table-multirow">
        <xsl:param name="current" select="/"/>
        <xsl:param name="field_name" select="@name"/>
        <td>
        	<xsl:choose>
        		<xsl:when test="self::field">
        			<xsl:value-of select="$current/@*[name()=$field_name]" disable-output-escaping="yes"/>        			
        		</xsl:when>
        		<xsl:otherwise>
        			<xsl:value-of select="." disable-output-escaping="yes"/>
        		</xsl:otherwise>
        	</xsl:choose>
        </td>
    </xsl:template>

    <!-- Query Vars -->
    <xsl:template match="BROWSER" mode="wbt:default-table">
    	<xsl:comment>Template Match 'wbt:default-table' BROWSER - <xsl:value-of select="$wbt:inc-name"/></xsl:comment>
    	<h3>BROWSER VARS</h3>
        <table class="table table-borders table-hover">
            <!-- detail -->
            <xsl:apply-templates mode="wbt:default-table"/>
        </table>
    </xsl:template>

    <xsl:template match="FORMVARS | QUERYVARS | COOKIEVARS | SESSIONVARS" mode="wbt:default-table">
    	<xsl:comment>Template Match 'wbt:default-table' <xsl:value-of select="name()"/> - <xsl:value-of select="$wbt:inc-name"/></xsl:comment>
    	<tr>
            <th colspan="2">
                <xsl:value-of select="name()"/>
            </th>
        </tr>
        <xsl:apply-templates select="ELEMENT" mode="wbt:default-table"/>
    </xsl:template>

    <xsl:template match="ELEMENT" mode="wbt:default-table">
        <tr>
            <td>
                <xsl:value-of select="@name"/>
            </td>
            <td>
                <xsl:value-of select="."/>
            </td>
        </tr>
    </xsl:template>

    <!-- tags -->
    <xsl:template match="TAGS" mode="wbt:default-table">
    	<xsl:comment>Template Match 'wbt:default-table' <xsl:value-of select="name()"/> - <xsl:value-of select="$wbt:inc-name"/></xsl:comment>
    	<h3>TAGS</h3>
        <table class="table table-borders table-hover">
            <!-- detail -->
            <xsl:apply-templates mode="wbt:default-table"/>
        </table>
    </xsl:template>

    <xsl:template match="TAG" mode="wbt:default-table">
        <tr>
            <td>
                <xsl:value-of select="@*[1]"/>
            </td>
            <td>
                <xsl:value-of select="."/>
            </td>
        </tr>
    </xsl:template>



</xsl:stylesheet>
