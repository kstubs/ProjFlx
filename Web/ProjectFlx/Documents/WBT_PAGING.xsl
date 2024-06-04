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
		<xsl:param name="id" select="concat('paging_',generate-id())"/>
		<xsl:param name="doc-action"/>
		<xsl:param name="limit" select="self::node()/limit"/>
		<xsl:apply-templates select="ancestor-or-self::results//paging" mode="wbt:paging">
			<xsl:with-param name="id" select="$id"/>
			<xsl:with-param name="doc-action" select="$doc-action"/>
		</xsl:apply-templates>
	</xsl:template>

	<xsl:template match="results" mode="wbt:paging">
		<xsl:apply-templates select="schema/query/paging" mode="wbt:paging"/>
	</xsl:template>
	
	<xsl:template match="paging" mode="wbt:paging">
		<xsl:param name="id" select="concat('paging_',generate-id())"/>
		<xsl:param name="doc-action"/>
		<xsl:param name="limit" select="self::node()/limit"/>
		<div class="paging">
			<form id="id_{$id}" name="{$id}" action="{$doc-action}" method="POST" class="p-2">
				<div class="row">
					<div class="col-auto">
						<div class="btn-group">
							<input class="btn btn-secondary" type="submit" name="PagingDirection" value="Previous" title="Previous Page"/>
							<input class="btn btn-secondary" type="submit" name="PagingDirection" value="Next" title="Next Page"/>
							<input class="btn btn-secondary" type="submit" name="PagingDirection" value="Top" title="Top Page"/>
							<input class="btn btn-secondary" type="submit" name="PagingDirection" value="Last" title="Last Page ({pages/@totalpages})"/>

							<select class="btn btn-secondary" id="pagePageSelector" name="PagingPage" onchange="$('id_{$id}').submit();">
								<xsl:apply-templates select="pages" mode="wbt:paging"/>
							</select>


							<select class="btn btn-secondary" id="pageLimitSelector" name="PagingLimit" onchange="$('id_{$id}').submit();">
								<xsl:call-template name="make-limits">
									<xsl:with-param name="items-per-page" select="$limit"/>
								</xsl:call-template>
							</select>
						</div>
					</div>
					<div class="col-auto">
						<xsl:text>Showing Page  </xsl:text>
						<xsl:value-of select="pages/@current"/>
						<xsl:text> of </xsl:text>
						<xsl:value-of select="pages/@totalpages"/>
						<xsl:text> Page(s)</xsl:text>
					</div>
				</div>
				<input type="hidden" name="PagingLastPage" value="{pages/@totalpages}"/>
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
		<xsl:param name="current" select="@current"/>
		<xsl:if test="not(page)">
			<option value="1">1</option>
		</xsl:if>
		<xsl:for-each select="page">
		<option value="{@value}">
			<xsl:if test="@value = $current">
				<xsl:attribute name="selected">selected</xsl:attribute>
			</xsl:if>
			<xsl:value-of select="@value"/>
		</option>
		</xsl:for-each>
	</xsl:template>
	
	<!-- convert browsersvars to hiddent browser vars -->
	<xsl:template name="wbt:AllBrowserVars_ToHiddenFormVars">
		<xsl:apply-templates select="/flx/proj/browser/formvars/element | /flx/proj/browser/queryvars/element" mode="wbt:HiddenVars"/>
	</xsl:template>
	
	<!-- surpress submit button from persistance-->
	<xsl:template priority="1" match="element[@name='hash'] | 
		element[@name='PagingPage'] | 
		element[@name='PagingLimit'] | 
		element[@name='PagingLastPage'] | 
		element[@name='PagingDirection']" mode="wbt:HiddenVars"/>
	
	<xsl:template match="element" mode="wbt:HiddenVars">
		<!-- make sure you get unique browser vars -->
		<xsl:if test="not(preceding::element[@name=current()/@name])">
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