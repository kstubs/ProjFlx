<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
	xmlns:wbt="myWebTemplater.1.0"
	exclude-result-prefixes="wbt">

	<wbt:limits>
		<limit>10</limit>
		<limit>25</limit>
		<limit>50</limit>
		<limit>100</limit>
		<limit>250</limit>
		<limit>500</limit>
		<limit>1000</limit>
	</wbt:limits>

	<!-- Create paging navigation context should be result -->
	<xsl:template name="wbt:paging">
		<xsl:apply-templates select="ancestor-or-self::results//paging" mode="wbt:paging"/>
	</xsl:template>

	<xsl:template match="results" mode="wbt:paging">
		<xsl:apply-templates select="schema/query/paging" mode="wbt:paging"/>
	</xsl:template>
	
	<xsl:template match="paging" mode="wbt:paging">
		<xsl:param name="id" select="concat('paging_',generate-id())"/>
		<xsl:param name="doc_action">#</xsl:param>
		<div class="paging">
			<form id="id_{$id}" name="{$id}" action="{$doc_action}" method="POST">
				<div>
					<input type="submit" name="direction" value="previous" title="Previous Page"/>
					<input type="submit" name="direction" value="next" title="Next Page"/>
					<input type="submit" name="direction" value="top" title="Top Page"/>
					<input type="submit" name="direction" value="last" title="Last Page ({@total_pages})"/>
				</div>
				<select id="pagePageSelector" name="page" onchange="$('id_{$id}').submit();">					
					<xsl:apply-templates select="pages" mode="wbt:paging">
						<xsl:with-param name="page" select="@current_page"/>
					</xsl:apply-templates>
				</select>
				<select id="pageLimitSelector" name="limit" onchange="$('id_{$id}').submit();">
					<xsl:call-template name="make-limits">
						<xsl:with-param name="items-per-page" select="@items_per_page"/>
					</xsl:call-template>
				</select>

				<!-- persist browser vars -->
				<xsl:call-template name="wbt:AllBrowserVars_ToHiddenFormVars"/>
			</form>
		</div>
	</xsl:template>


	<xsl:template name="make-limits">
		<xsl:param name="items-per-page"/>
		<xsl:for-each select="document('')//wbt:limits/limit">
			<option value="{.}">
				<xsl:if test="$items-per-page = .">
					<xsl:attribute name="selected">selected</xsl:attribute>
				</xsl:if>
				<xsl:text>Limit </xsl:text>
				<xsl:value-of select="."/>
			</option>
		</xsl:for-each>
		
	</xsl:template>
	
	<xsl:template match="pages" mode="wbt:paging">
		<xsl:param name="page" />
		<xsl:if test="not(page)">
			<option value="1">1</option>
		</xsl:if>
		<xsl:for-each select="page">
		<option value="{@value}">
			<xsl:if test="@value = $page">
				<xsl:attribute name="selected">selected</xsl:attribute>
			</xsl:if>
			<xsl:value-of select="."/>
		</option>
		</xsl:for-each>
	</xsl:template>
	
	<!-- convert browsersvars to hiddent browser vars -->
	<xsl:template name="wbt:AllBrowserVars_ToHiddenFormVars">
		<xsl:apply-templates select="/ROOT/TMPLT/BROWSER/node()
			[name()='FORMVARS' or name()='QUERYVARS']/ELEMENT" mode="wbt:HiddenVars"/>
	</xsl:template>
	
	<!-- surpress submit button from persistance-->
	<xsl:template priority="1" match="ELEMENT[@name='hash'] | 
						 ELEMENT[@name='limit'] | 
						 ELEMENT[@name='lastpage'] | 
						 ELEMENT[@name='direction'] | 
						 ELEMENT[@name='page'] | 
						 ELEMENT[@name='pagingrequest']" mode="wbt:HiddenVars"/>
	
	<xsl:template match="ELEMENT" mode="wbt:HiddenVars">
		<!-- make sure you get unique browser vars -->
		<xsl:if test="not(preceding::ELEMENT[@name=current()/@name])">
			<xsl:element name="input">
				<xsl:attribute name="name">
					<xsl:value-of select="@name"/>
				</xsl:attribute>
				<xsl:attribute name="type">hidden</xsl:attribute>
				<xsl:attribute name="value">
					<xsl:value-of select="."/>
				</xsl:attribute>
			</xsl:element>
		</xsl:if>
	</xsl:template>
	
	
</xsl:stylesheet>