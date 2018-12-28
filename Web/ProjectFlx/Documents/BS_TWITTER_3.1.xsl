<?xml version="1.0" encoding="UTF-8"?>
<xsl:stylesheet xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
	xmlns:xs="http://www.w3.org/2001/XMLSchema"
	xmlns:bs="twitterBootStrap3.1"
	exclude-result-prefixes="xs"
	version="2.0">
	

	<xsl:template match="bs:panel" name="bs:panel" mode="identity-translate">
		<div>
			<xsl:attribute name="class">
				<xsl:text>panel panel-default </xsl:text>
				<xsl:value-of select="@class"/>
			</xsl:attribute>
			<xsl:apply-templates select="@*[not(local-name()='class')]" mode="identity-translate"></xsl:apply-templates>
			<xsl:if test="@title">
				<div class="panel-heading">
					<h1>
						<xsl:value-of select="@title"/>
					</h1>
				</div>
			</xsl:if>
			<div class="panel-body">
				<xsl:apply-templates select="node() | *" mode="identity-translate"/>
			</div>
		</div>		
	</xsl:template>
	
</xsl:stylesheet>