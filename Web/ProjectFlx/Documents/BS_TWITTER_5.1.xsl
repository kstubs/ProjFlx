<?xml version="1.0" encoding="UTF-8"?>
<xsl:stylesheet xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
	xmlns:xs="http://www.w3.org/2001/XMLSchema"
	xmlns:bs="twitterBootStrap3.1"
	exclude-result-prefixes="xs"
	version="2.0">
	

	<xsl:template match="bs:panel" name="bs:panel" mode="identity-translate">
		<xsl:call-template name="bs:card"/>
	</xsl:template>

	<xsl:template match="bs:card" name="bs:card" mode="identity-translate">
		<div>
			<xsl:attribute name="class">
				<xsl:text>card card-secondary</xsl:text>
				<xsl:value-of select="@class"/>
			</xsl:attribute>
			<xsl:apply-templates select="@*[not(local-name()='class')]" mode="identity-translate"/>
			<div class="card-body">
				<xsl:if test="@title">
					<h3 class="card-title">
						<xsl:value-of select="@title"/>
					</h3>
				</xsl:if>
				<xsl:apply-templates select="node() | *" mode="identity-translate"/>
			</div>
		</div>		
	</xsl:template>
	
</xsl:stylesheet>